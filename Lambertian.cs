internal record Lambertian( Vector Albedo ) : IMaterial
{
	public bool Scatter( Ray inboundRay, Hit objectHit, out Vector attenuation, out Ray scatteredRay )
	{
		var scatterDirection = objectHit.Normal + Vector.RandomNormalized();
		if( scatterDirection.NearZero )
		{
			scatterDirection = objectHit.Normal;
		}
		scatteredRay = new Ray( objectHit.Location, scatterDirection );
		attenuation = Albedo;

		return true;
	}
}