namespace RTree;

public class HitByRay : ICheck
{
	private readonly double[] _origin;
	private readonly double[] _direction;

	public HitByRay( ReadOnlySpan<double> origin, ReadOnlySpan<double> direction )
	{
		_origin = origin.ToArray();
		_direction = direction.ToArray();
	}

	public bool Check( ReadOnlySpan<double> box_min, ReadOnlySpan<double> box_max )
	{
		// based on Fast Ray-Box Intersection
		// by Andrew Woo
		// from "Graphics Gems", Academic Press, 1990

		var inside = true;
		Span<int> quadrant = stackalloc int[_origin.Length];
		Span<double> maxT = stackalloc double[_origin.Length];
		Span<double> candidatePlane = stackalloc double[_origin.Length];
		Span<double> coordinate = stackalloc double[_origin.Length];
		maxT.Clear();
		candidatePlane.Clear();
		coordinate.Clear();
		for( int i = 0; i < _origin.Length; i++ )
		{
			if( _origin[i] < box_min[i] )
			{
				quadrant[i] = -1;
				candidatePlane[i] = box_min[i];
				inside = false;
			}
			else if( _origin[i] > box_max[i] )
			{
				quadrant[i] = 1;
				candidatePlane[i] = box_max[i];
				inside = false;
			}
			else
				quadrant[i] = 0;
		}

		if( inside )
		{
			// coordinate = m_origin;
			return true;
		}

		for( int i = 0; i < _origin.Length; i++ )
		{
			if( quadrant[i] != 0 && _direction[i] != 0 )
				maxT[i] = (candidatePlane[i] - _origin[i]) / _direction[i];
			else
				maxT[i] = -1;
		}

		var which_plane = 0;

		for( int i = 1; i < _origin.Length; i++ )
		{
			if( maxT[which_plane] < maxT[i] )
				which_plane = i;
		}

		if( maxT[which_plane] < 0 )
			return false;

		for( int i = 0; i < _origin.Length; i++ )
		{
			if( which_plane != i )
			{
				coordinate[i] = _origin[i] + maxT[which_plane] * _direction[i];

				if( coordinate[i] < box_min[i] || coordinate[i] > box_max[i] )
					return false;
			}
			else
				coordinate[i] = candidatePlane[i];
		}

		return true;
	}
};
