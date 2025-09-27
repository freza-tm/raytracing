using System;
using System.Collections.Generic;
using System.Linq;

namespace RTree;

/// <summary>
/// Represents static R-Tree implemented <see href="https://en.wikipedia.org/wiki/Hilbert_R-tree#Packed_Hilbert_R-trees"/>
/// Inspired by <see href="https://github.com/oberbichler/Cage"/>
/// </summary>
/// <typeparam name="TTag">Type of the tag attached to item volume</typeparam>
public class PackedHilbertRTree<TTag>
{
	#region Fields

	private readonly int _nodeChildCount;
	private readonly List<int> _nodeLevelBounds = [];
	private readonly double[] _overallBounds;
	private readonly TTag[] _tags;
	private readonly int[] _indices;

	/// <summary>
	/// Contains bounding volumes of items in the tree, each volume is described by 2 points, first are the minimum coordinates, second are the maximum coordinates (total 2*<see cref="Dimension"/> coordinates)
	/// </summary>
	private readonly double[] _itemsRectangles;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates new instance of <see cref="PackedHilbertRTree{T}"/>
	/// </summary>
	/// <param name="treeDimension">The number of dimensions (must be more than 1)</param>
	/// <param name="itemsCount">The number of items to be put to the tree</param>
	/// <param name="nodeChildCount">The maximum number of child nodes each parent node has</param>
	public PackedHilbertRTree( int treeDimension, int itemsCount, int nodeChildCount = 16 )
	{
		ArgumentOutOfRangeException.ThrowIfLessThan( treeDimension, 1 );
		ArgumentOutOfRangeException.ThrowIfLessThan( nodeChildCount, 2 );

		Dimension = treeDimension;
		_overallBounds = new double[treeDimension * 2];

		NumberOfItems = itemsCount;
		_nodeChildCount = Math.Min( Math.Max( nodeChildCount, 2 ), int.MaxValue );

		var n = NumberOfItems;
		var nodesCount = n;

		_nodeLevelBounds.Add( n );

		do
		{
			n = (int) Math.Ceiling( (double) n / _nodeChildCount );
			nodesCount += n;
			_nodeLevelBounds.Add( nodesCount );
		}
		while( n > 1 );

		_tags = new TTag[itemsCount];
		_indices = new int[nodesCount - itemsCount];
		_itemsRectangles = new double[nodesCount * treeDimension * 2];
		InitializeRect( _overallBounds );
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the number of dimensions of the tree
	/// </summary>
	public int Dimension { get; }

	/// <summary>
	/// Gets the number of items in the tree
	/// </summary>
	public int NumberOfItems { get; }

	/// <summary>
	/// Gets total number of nodes in the tree
	/// </summary>
	private int NodesCount => _tags.Length + _indices.Length;

	#endregion

	#region Methods

	/// <summary>
	/// Fills the tree with items
	/// </summary>
	/// <param name="rectangles">The items to insert to the tree. Each item is two points describing N-dimensional volume (2*N coordinates) and tag to identify the item</param>
	/// <exception cref="InvalidOperationException">The number of items inserted does not match the <see cref="NumberOfItems"/> value</exception>
	/// <exception cref="ArgumentException">The inserted rectangle dimensions did not match the tree dimension</exception>
	public void Fill( IEnumerable<(double[] Coordinates, TTag Tag)> rectangles ) => Fill( rectangles.Select( r => ((ReadOnlyMemory<double>) r.Coordinates.AsMemory(), r.Tag) ) );

	/// <summary>
	/// Fills the tree with items
	/// </summary>
	/// <param name="rectangles">The items to insert to the tree. Each item is two points describing N-dimensional volume (2*N coordinates) and tag to identify the item</param>
	/// <exception cref="InvalidOperationException">The number of items inserted does not match the <see cref="NumberOfItems"/> value</exception>
	/// <exception cref="ArgumentException">The inserted rectangle dimensions did not match the tree dimension</exception>
	public void Fill( IEnumerable<(ReadOnlyMemory<double> Coordinates, TTag Tag)> rectangles )
	{
		var index = 0;

		Span<double> itemBounds = stackalloc double[Dimension * 2];

		foreach( (ReadOnlyMemory<double> rectangle, TTag tag) in rectangles )
		{
			if( index > NumberOfItems )
				throw new InvalidOperationException( "Too many items" );

			_tags[index] = tag;
			ReadBounds( rectangle.Span, ref itemBounds );
			UpdateBounds( itemBounds, _overallBounds );
			SetRectangleAt( index, itemBounds );
			index++;
		}

		if( index != NumberOfItems )
			throw new InvalidOperationException( "Not enough items" );

		ConstructParentNodes();

		void ReadBounds( ReadOnlySpan<double> rectangle, ref Span<double> bounds )
		{
			if( rectangle.Length != Dimension * 2 )
				throw new ArgumentException( "The dimensions of the rectangle do not match the dimension of the tree." );

			for( var dimension = 0; dimension < Dimension; dimension++ )
			{
				bounds[dimension] = Math.Min( rectangle[dimension], rectangle[dimension + Dimension] );
				bounds[dimension + Dimension] = Math.Max( rectangle[dimension], rectangle[dimension + Dimension] );
			}
		}
	}

	internal IEnumerable<(ReadOnlyMemory<double> Coordinates, TTag Tag)> DumpAllItems() => _tags
		.Select( ( tag, index ) => (GetRectangleMemoryAt( index ), tag) );

	private void InitializeRect( Span<double> rectangle )
	{
		for( var dimension = 0; dimension < Dimension; dimension++ )
		{
			rectangle[dimension] = double.PositiveInfinity;
			rectangle[dimension + Dimension] = double.NegativeInfinity;
		}
	}

	private void UpdateBounds( ReadOnlySpan<double> itemRect, Span<double> bounds )
	{
		for( var dimension = 0; dimension < Dimension; dimension++ )
		{
			if( itemRect[dimension] < bounds[dimension] )
				bounds[dimension] = itemRect[dimension];

			if( itemRect[dimension + Dimension] > bounds[dimension + Dimension] )
				bounds[dimension + Dimension] = itemRect[dimension + Dimension];
		}
	}

	/// <summary>
	/// Returns the bounding rectangle of the tree item at given index
	/// </summary>
	/// <param name="index">The index of the item</param>
	/// <seealso cref="SearchForPositions"/>
	public ReadOnlySpan<double> GetRectangleAt( int index ) => _itemsRectangles.AsSpan( index * Dimension * 2, Dimension * 2 );

	/// <summary>
	/// Returns the tag of the tree item at given index
	/// </summary>
	/// <param name="index">The index of the item</param>
	/// <seealso cref="SearchForPositions"/>
	public TTag GetTagAt( int index ) => _tags[index];

	/// <summary>
	/// Replaces the tag of the tree item at given index
	/// </summary>
	/// <param name="index">The index of the item</param>
	/// <param name="tag">The new tag to set</param>
	/// <returns>The old tag at <paramref name="index"/></returns>
	public TTag ReplaceTagAt( int index, TTag tag )
	{
		var oldTag = _tags[index];

		_tags[index] = tag;

		return oldTag;
	}

	private ReadOnlyMemory<double> GetRectangleMemoryAt( int index ) => _itemsRectangles.AsMemory( index * Dimension * 2, Dimension * 2 );

	private void SetRectangleAt( int index, ReadOnlySpan<double> rectangle ) => rectangle.CopyTo( _itemsRectangles.AsSpan( index * Dimension * 2 ) );

	private void Sort( ulong[] values, int left, int right )
	{
		while( true )
		{
			if( left >= right )
				return;

			var pivot = values[(left + right) >> 1];

			var i = left - 1;
			var j = right + 1;

			while( true )
			{
				do
				{
					i += 1;
				}
				while( values[i] < pivot );

				do
				{
					j -= 1;
				}
				while( values[j] > pivot );

				if( i >= j )
					break;

				Swap( values, i, j );
			}

			Sort( values, left, j );
			left = j + 1;
		}
	}

	private void Swap( ulong[] values, int i, int j )
	{
		(values[i], values[j]) = (values[j], values[i]);
		(_tags[i], _tags[j]) = (_tags[j], _tags[i]);

		Span<double> temp = stackalloc double[Dimension * 2];

		GetRectangleAt( i ).CopyTo( temp );
		SetRectangleAt( i, GetRectangleAt( j ) );
		SetRectangleAt( j, temp );
	}

	private void SetParentNodeFirstChildIndex( int parentNodeIndex, int childNodeIndex ) => _indices[parentNodeIndex - NumberOfItems] = childNodeIndex;

	private int GetParentNodeFirstChildIndex( int parentNodeIndex ) => _indices[parentNodeIndex - NumberOfItems];

	private void ConstructParentNodes()
	{
		var hilbert = new HilbertIndex( Dimension );
		var size = new double[Dimension];

		for( var j = 0; j < Dimension; j++ )
		{
			size[j] = (_overallBounds[j + Dimension] - _overallBounds[j]) * 1.01;
		}

		var hilbertValues = new ulong[NumberOfItems];

		Span<ulong> center = stackalloc ulong[3];

		for( var i = 0; i < NumberOfItems; i++ )
		{
			var itemBox = GetRectangleAt( i );

			for( var j = 0; j < Dimension; j++ )
			{
				center[j] = (ulong) (((itemBox[j] + itemBox[Dimension + j]) / 2 - _overallBounds[j]) / size[j] * hilbert.MaxAxisSize);
			}

			hilbertValues[i] = hilbert.IndexAt( center );
		}

		Sort( hilbertValues, 0, NumberOfItems - 1 );

		var pos = 0;
		var parentNodeIndex = NumberOfItems;

		Span<double> nodeBounds = stackalloc double[Dimension * 2];

		for( var i = 0; i < _nodeLevelBounds.Count - 1; i++ )
		{
			var end = _nodeLevelBounds[i];

			while( pos < end )
			{
				InitializeRect( nodeBounds );

				var nodeIndex = pos;

				for( var j = 0; j < _nodeChildCount && pos < end; j++ )
				{
					var rectangle = GetRectangleAt( pos );

					pos += 1;

					UpdateBounds( rectangle, nodeBounds );
				}

				SetParentNodeFirstChildIndex( parentNodeIndex, nodeIndex );

				SetRectangleAt( parentNodeIndex, nodeBounds );

				parentNodeIndex += 1;
			}
		}
	}

	/// <summary>
	/// Performs search in the tree using provided check object to filter results
	/// </summary>
	/// <param name="check">The object to check item volume for match</param>
	/// <returns>Collection of item positions that satisfy given <paramref name="check"/></returns>
	/// <seealso cref="GetRectangleAt"/>
	/// <seealso cref="GetTagAt"/>
	/// <seealso cref="ReplaceTagAt"/>
	public IEnumerable<int> SearchForPositions( ICheck check )
	{
		if( NumberOfItems == 0 )
			yield break;

		if( _indices.Length == 0 )
			throw new InvalidOperationException( "The tree is not filled!" );

		var nodeIndex = NodesCount - 1;
		var level = _nodeLevelBounds.Count - 1;
		Stack<(int Index, int Level)> nextNodesStack = new();

		while( true )
		{
			var end = Math.Min( nodeIndex + _nodeChildCount, _nodeLevelBounds[level] );

			for( var pos = nodeIndex; pos < end; pos++ )
			{
				var bounds = GetRectangleAt( pos );

				if( !check.Check( bounds.Slice( 0, Dimension ), bounds.Slice( Dimension, Dimension ) ) )
					continue;

				if( nodeIndex < NumberOfItems )
				{
					yield return pos;
				}
				else
				{
					nextNodesStack.Push( (GetParentNodeFirstChildIndex( pos ), level - 1) );
				}
			}

			if( !nextNodesStack.TryPop( out var nextNode ) )
				yield break;

			(nodeIndex, level) = nextNode;
		}
	}

	/// <summary>
	/// Performs search in the tree using provided check object to filter results
	/// </summary>
	/// <param name="check">The object to check item volume for match</param>
	/// <returns>Collection of item volume and tags that satisfy given <paramref name="check"/></returns>
	internal IEnumerable<(ReadOnlyMemory<double> Coordinates, TTag Tag)> Search( ICheck check ) => SearchForPositions( check ).Select( pos => (GetRectangleMemoryAt( pos ), GetTagAt( pos )) );

	#endregion
}
