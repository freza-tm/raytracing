internal record Dielectric( double RefractionIndex, double Fuzziness ) : IMaterial
{
	public bool Scatter( Ray inboundRay, Hit objectHit, out Vector attenuation, out Ray scatteredRay )
	{
		attenuation = new Vector( 1, 1, 1 );
		double ri = objectHit.FrontFace ? 1.0 / RefractionIndex : RefractionIndex;

		var unitDirection = inboundRay.Direction.Normalized();
		var normal = Fuzziness == 0.0 ? objectHit.Normal : (objectHit.Normal + Fuzziness * Vector.RandomNormalized()).Normalized();

		double cosTheta = Math.Min( unitDirection.Dot( -normal ), 1.0 );
		double sinTheta = Math.Sqrt( 1.0 - cosTheta * cosTheta );

		bool cannotRefract = ri * sinTheta > 1.0;
		var direction = cannotRefract || Reflectance( cosTheta, ri ) > System.Random.Shared.NextDouble()
		? unitDirection.Reflect( normal )
		: unitDirection.Refract( normal, ri );

		scatteredRay = new Ray( objectHit.Location, direction );

		return true;

		static double Reflectance( double cosine, double refractionIndex )
		{
			// Use Schlick's approximation for reflectance.
			var r0 = (1 - refractionIndex) / (1 + refractionIndex);
			r0 = r0 * r0;
			return r0 + (1 - r0) * Math.Pow( (1 - cosine), 5 );
		}
	}
}