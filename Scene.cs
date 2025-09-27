using RTree;

internal sealed class Scene : IHittable
{
	private readonly List<IHittable> _items = [];

	private PackedHilbertRTree<IHittable>? _sceneLookup;

	public void Add( IHittable element ) => _items.Add( element );

	public void InitializeLookup()
	{
		_sceneLookup = new PackedHilbertRTree<IHittable>( 3, _items.Count, 4 );
		_sceneLookup.Fill( _items.Select( item=>(item.GetBounds(), item) )  );
	}

	public bool IsHitByRay( Ray ray, double tMin, double tMax, out Hit hit )
	{
		hit = Hit.NoHit;
		bool didHit = false;
		double nearestHit = tMax;

		var check = new HitByRay( [ray.Origin.X, ray.Origin.Y, ray.Origin.Z], [ray.Direction.X, ray.Direction.Y, ray.Direction.Z] );

		foreach( var candidate in 		_sceneLookup!.Search( check ) )
		{
			if( candidate.Tag.IsHitByRay( ray, tMin, nearestHit, out var elementHit ) )
			{
				nearestHit = elementHit.T;
				didHit = true;
				hit = elementHit;
			}
		}

		return didHit;
	}

	public double[] GetBounds() => throw new NotImplementedException();
}
