using System;
using System.Collections.Generic;

namespace RTree;

/// <summary>
/// Represents test for finding items in R-Tree that have bounds intersecting with given bounding volume
/// </summary>
public class WithinVolume : ICheck
{
	#region Fields

	private readonly double[] _box;

	#endregion

	#region Constructors

	/// <summary>
	/// Creates new instance of <see cref="WithinVolume"/>
	/// </summary>
	/// <param name="a">The coordinates of one point of bounding volume</param>
	/// <param name="b">The coordinates of other point of bounding volume</param>
	/// <exception cref="ArgumentOutOfRangeException">The dimensions of points do not match or are below 2</exception>
	public WithinVolume( ReadOnlySpan<double> a, ReadOnlySpan<double> b )
	{
		ArgumentOutOfRangeException.ThrowIfLessThan( a.Length, 2, nameof( a ) );
		ArgumentOutOfRangeException.ThrowIfLessThan( b.Length, a.Length, nameof( b ) );

		_box = new double[a.Length * 2];

		for( var i = 0; i < a.Length; i++ )
		{
			(_box[i], _box[i + a.Length]) = a[i] < b[i]
				? (a[i], b[i])
				: (b[i], a[i]);
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// Returns collection of items in given tree that have bounds intersecting with bounding volume
	/// </summary>
	/// <param name="tree">The tree to search in</param>
	public IEnumerable<(ReadOnlyMemory<double> Coordinates, TTag Tag)> Find<TTag>( PackedHilbertRTree<TTag> tree )
	{
		ArgumentOutOfRangeException.ThrowIfNotEqual( tree.Dimension, _box.Length / 2, nameof( tree ) );

		return tree.Search( this );
	}

	#endregion

	#region ICheck Members

	public bool Check( ReadOnlySpan<double> min, ReadOnlySpan<double> max )
	{
		for( var i = 0; i < min.Length; i++ )
		{
			if( _box[i + min.Length] < min[i] )
				return false;

			if( _box[i] > max[i] )
				return false;
		}

		return true;
	}

	#endregion
}
