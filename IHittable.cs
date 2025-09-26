internal interface IHittable
{
	bool IsHitByRay( Ray ray, double tMin, double tMax, out Hit hit );
}