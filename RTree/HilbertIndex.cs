using System;
using System.Numerics;

namespace RTree;

/// <summary>
/// Represents projection from N-dimensional coordinates to 1-dimensional coordinate using N-dimensional Hilbert curve
/// Inspired by <see href="https://github.com/oberbichler/Cage"/> this algorithm seems to be quite fast, not depending on size of the Hilbert curve much
/// </summary>
internal class HilbertIndex
{
	#region Constructors

	/// <summary>
	/// Creates new instance of <see cref="HilbertIndex"/>
	/// </summary>
	/// <param name="dimension">Number of dimensions</param>
	public HilbertIndex( int dimension )
	{
		Dimension = dimension;
		MaxM = 8 * sizeof( ulong ) / Dimension;
		MaxAxisSize = (ulong) 1 << MaxM;
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the number of dimensions
	/// </summary>
	public int Dimension { get; }

	/// <summary>
	/// Gets the maximum coordinate value along single axis
	/// </summary>
	public ulong MaxAxisSize { get; }

	/// <summary>
	/// Gets the number of bits per dimension
	/// </summary>
	private int MaxM { get; }

	#endregion

	#region Methods

	private static int Log2( ulong value ) => BitOperations.Log2( value );

	private ulong Rol( ulong x, int d )
	{
		int c = d % Dimension >= 0 ? d % Dimension : d % Dimension + Dimension;

		return (x << c | x >> (Dimension - c)) & (((ulong) 1 << Dimension) - 1);
	}

	private ulong Ror( ulong x, int d )
	{
		int c = d % Dimension >= 0 ? d % Dimension : d % Dimension + Dimension;

		return (x >> c | x << (Dimension - c)) & (((ulong) 1 << Dimension) - 1);
	}

	private static ulong Bit( ulong x, int i ) => x >> i & 1;

	private static ulong Gc( ulong i ) => i ^ (i >> 1);

	private static ulong E( ulong i ) => i == 0 ? 0 : Gc( ((i - 1) >> 1) << 1 );

	private ulong InverseGc( ulong gc )
	{
		ulong i = gc;
		int j = 1;

		while( j < Dimension )
		{
			i ^= gc >> j;
			j += 1;
		}

		return i;
	}

	private static int G( ulong i ) => Log2( Gc( i ) ^ Gc( i + 1 ) );

	private int D( ulong i ) => i switch
	{
		0 => 0,
		_ when (i & 1) == 0 => G( i - 1 ) % Dimension,
		_ => G( i ) % Dimension
	};

	private ulong T( ulong e, int d, ulong b ) => Ror( b ^ e, d + 1 );

	private ulong IndexAt( int m, ReadOnlySpan<ulong> p )
	{
		ulong h = 0;
		ulong ve = 0;
		int vd = 0;

		for( int i = m - 1; i > -1; i-- )
		{
			ulong s = 0;

			for( int j = 0; j < Dimension; j++ )
			{
				s += Bit( p[j], i ) << j;
			}

			ulong l = T( ve, vd, s );
			ulong w = InverseGc( l );

			ve ^= (Rol( E( w ), vd + 1 ));
			vd = (vd + D( w ) + 1) % Dimension;
			h = (h << Dimension) | w;
		}

		return h;
	}

	/// <summary>
	/// Returns the distance along the Hilbert curve for the given point
	/// </summary>
	/// <param name="p">The point in coordinates of the space</param>
	/// <returns>The distance along the Hilbert curve</returns>
	public ulong IndexAt( ReadOnlySpan<ulong> p ) => IndexAt( MaxM, p );

	#endregion
}
