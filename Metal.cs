internal record Metal( Vector Albedo, double Fuzziness ) : IMaterial
{
	public bool Scatter( Ray inboundRay, Hit objectHit, out Vector attenuation, out Ray scatteredRay )
	{
		var normal = Fuzziness == 0.0 ? objectHit.Normal : (objectHit.Normal + Fuzziness * Vector.RandomNormalized()).Normalized();

		var reflected = inboundRay.Direction.Reflect( normal );
		scatteredRay = new Ray( objectHit.Location, reflected );
		attenuation = Albedo;

		return scatteredRay.Direction.Dot( objectHit.Normal ) > 0;
	}
}
