internal sealed class Sphere( Vector Center, double Radius, IMaterial Material ) : IHittable
{
	public bool IsHitByRay( Ray ray, double tMin, double tMax, out Hit hit )
	{
		hit = Hit.NoHit;

		var oc = Center - ray.Origin;
		var a = ray.Direction.MagnitudeSquared;
		var h = ray.Direction.Dot( oc );
		var c = oc.MagnitudeSquared - Radius * Radius;
		var discriminant = h * h - a * c;

		if( discriminant < 0 )
			return false;

		var sqrtd = Math.Sqrt( discriminant );

		if( a == 0 )
			System.Diagnostics.Debugger.Break();

		// Find the nearest root that lies in the acceptable range.
		var root = (h - sqrtd) / a;
		if( root <= tMin || tMax <= root )
		{
			root = (h + sqrtd) / a;
			if( root <= tMin || tMax <= root )
				return false;
		}

		var location = ray.At( root );

		var outwardNormal = (location - Center) / Radius;

		(bool frontFace, Vector localNormal) = outwardNormal.Dot( ray.Direction ) < 0
			? (true, outwardNormal)
			: (false, -outwardNormal);

		hit = new Hit( location, localNormal, frontFace, root, Material );

		return true;
	}

	public double[] GetBounds()=>
	[
		Center.X - Radius, Center.Y - Radius, Center.Z - Radius,
		Center.X + Radius, Center.Y + Radius, Center.Z + Radius
	];
}
