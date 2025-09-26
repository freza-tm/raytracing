internal sealed class Scene : IHittable
{
	private readonly List<IHittable> _items = [];

	public void Add( IHittable element ) => _items.Add( element );

	public bool IsHitByRay( Ray ray, double tMin, double tMax, out Hit hit )
	{
		hit = Hit.NoHit;
		bool didHit = false;
		double nearestHit = tMax;

		foreach( var element in _items )
		{
			if( element.IsHitByRay( ray, tMin, nearestHit, out var elementHit ) )
			{
				nearestHit = elementHit.T;
				didHit = true;
				hit = elementHit;
			}
		}

		return didHit;
	}
}