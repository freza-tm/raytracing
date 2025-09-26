internal interface IMaterial
{
	bool Scatter( Ray inboundRay, Hit objectHit, out Vector attenuation, out Ray scatteredRay );
}