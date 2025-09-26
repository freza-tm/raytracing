internal sealed class Camera( Vector Eye, Vector Target, Vector Up, int ImageWidth, int ImageHeight, Scene Scene )
{
	private int _maxRayRecursionDepth = 50;
	private double _verticalFov = 20.0 / 180.0 * Math.PI;
	private double _defocusAngle = 10.0 / 180.0 * Math.PI;

	public double FovInDegrees
	{
		get => _verticalFov * 180.0 / Math.PI;
		init => _verticalFov = value / 180.0 * Math.PI;
	}

	public double DefocusAngleInDegrees
	{
		get => _defocusAngle * 180.0 / Math.PI;
		init => _defocusAngle = value / 180.0 * Math.PI;
	}

	public int SamplesPerPixel { get; init; } = 1;

	public OutputImage GenerateImage()
	{
		var image = new OutputImage( ImageWidth, ImageHeight );

		var focalLength = (Eye - Target).Magnitude;

		var w = (Eye - Target).Normalized();
		var u = Up.Cross( w ).Normalized();
		var v = w.Cross( u );

		double viewportHeight = 2.0 * Math.Tan( _verticalFov / 2.0 ) * focalLength;
		double viewportWidth = viewportHeight * ((double) ImageWidth / ImageHeight);

		var viewportU = viewportWidth * u;
		var viewportV = -viewportHeight * v;
		var pixelDeltaU = viewportU / ImageWidth;
		var pixelDeltaV = viewportV / ImageHeight;

		var defocusRadius = focalLength * Math.Tan( _defocusAngle / 2.0 );
		var defocusDiskU = u * defocusRadius;
		var defocusDiskV = v * defocusRadius;

		var viewportTopLeft = Eye - focalLength * w - viewportU / 2 - viewportV / 2;
		var topLeftPixelCenter = viewportTopLeft + 0.5 * (pixelDeltaU + pixelDeltaV);

		image.SetPixels( GeneratePixels );


		return image;

		IEnumerable<Pixel> GeneratePixels( int row )
		{
			for( int x = 0; x < ImageWidth; x++ )
			{
				var pixelCenter = topLeftPixelCenter + x * pixelDeltaU + row * pixelDeltaV;
				var pixelColor = new Vector();
				for( int sample = 0; sample < SamplesPerPixel; sample++ )
				{
					pixelColor += GetRayColor( GetRay( sample, pixelCenter ) );
				}
				yield return new Pixel( pixelColor / SamplesPerPixel );
			}
		}

		Ray GetRay( int sample, Vector pixelCenter )
		{
			if( sample == 0 )
				return new Ray( Eye, pixelCenter - Eye );

			(var sampleU, var sampleV, _) = SampleSquare();

			var pixelSample = pixelCenter + sampleU * pixelDeltaU + sampleV * pixelDeltaV;
			var rayOrigin = _defocusAngle <= 0 ? Eye : DefocusDiscSample();

			return new Ray( rayOrigin, pixelSample - rayOrigin );
		}

		Vector SampleSquare() => new Vector( System.Random.Shared.NextDouble() - 0.5, System.Random.Shared.NextDouble() - 0.5, 0 );

		Vector DefocusDiscSample()
		{
			(var sampleU, var sampleV, _) = Vector.RandomInDisc();

			return Eye + defocusDiskU * sampleU + defocusDiskV * sampleV;
		}
	}


	private Vector GetRayColor( Ray r ) => GetRayColor( r, 0 );

	private Vector GetRayColor( Ray r, int depth )
	{
		if( depth > _maxRayRecursionDepth )
			return new Vector();

		if( Scene.IsHitByRay( r, 1e-5, double.PositiveInfinity, out var hit ) )
		{
			if( hit.Material.Scatter( r, hit, out var attenuation, out var scatteredRay ) )
				return attenuation.Hadamard( GetRayColor( scatteredRay, depth + 1 ) );

			return attenuation;
		}

		var a = 0.5 * (r.Direction.Normalized().Y + 1.0);
		return (1.0 - a) * new Vector( 1.0, 1.0, 1.0 ) + a * new Vector( 0.5, 0.7, 1.0 );
	}

}