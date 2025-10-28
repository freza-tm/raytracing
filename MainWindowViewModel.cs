using System.ComponentModel;
using System.Windows.Input;
using Avalonia.Media;

namespace Raytracer;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
	private DateTime _imageUpdateTimestamp = DateTime.UtcNow;

	public MainWindowViewModel()
	{
		Start = new RelayCommand( StartGeneration, null );
	}

	public ICommand Start { get; }

	public IImage? Image { get; private set; }

	public int Samples { get; private set; }

	public event PropertyChangedEventHandler? PropertyChanged;

	private void StartGeneration( object? parameter )
	{
		Task.Run( GenerationLoop );
	}

	private void GenerationLoop()
	{
		const int Width = 1024;
		const int Height = 768;
		const int SamplesPerPixel = 1;

		var camera = BigScene();

		var image = new OutputImage( Width, Height );
		image.BitmapUpdated += OnImageGenerated;

		Camera SmallScene()
		{
			var scene = new Scene();
			var materialGround = new Lambertian( new Vector( 0.8, 0.8, 0.0 ) );
			var materialCenter = new Lambertian( new Vector( 0.1, 0.2, 0.5 ) );
			var materialLeft = new Dielectric( 1.5, 0.0 );
			var materialBubble = new Dielectric( 1.0 / 1.5, 0.0 );
			var materialRight = new Metal( new Vector( 0.8, 0.6, 0.2 ), 0.01 );

			scene.Add( new Sphere( new Vector( 0, -100.5, -1 ), 100.0, materialGround ) );
			scene.Add( new Sphere( new Vector( 0, 0, -1.2 ), 0.5, materialCenter ) );
			scene.Add( new Sphere( new Vector( -1.0, 0, -1.0 ), 0.5, materialLeft ) );
			scene.Add( new Sphere( new Vector( -1.0, 0, -1.0 ), 0.4, materialBubble ) );
			scene.Add( new Sphere( new Vector( 1.0, 0, -1.0 ), 0.5, materialRight ) );
			scene.InitializeLookup();

			return new Camera( new Vector( -2, 2, 1 ), new Vector( 0, 0, -1 ), new Vector( 0, 1, 0 ), Width, Height, scene )
			{
				FovInDegrees = 20.0,
				DefocusAngleInDegrees = 10.0,
				SamplesPerPixel = SamplesPerPixel
			};
		}

		double RandomDouble() => System.Random.Shared.NextDouble();

		Camera BigScene()
		{
			var scene = new Scene();
			var ground = new Lambertian( new Vector( 0.5, 0.5, 0.5 ) );
			scene.Add( new Sphere( new Vector( 0, -1000, 0 ), 1000, ground ) );

			for( int a = -11; a < 11; a++ )
			{
				for( int b = -11; b < 11; b++ )
				{
					var matPick = System.Random.Shared.NextDouble();
					var center = new Vector( a + 0.9 * RandomDouble(), 0.2, b + 0.9 * RandomDouble() );

					if( (center - new Vector( 4, 0.2, 0 )).Magnitude > 0.9 )
					{
						if( matPick < 0.8 )
						{
							// diffuse
							var albedo = Vector.Random().Hadamard( Vector.Random() );
							scene.Add( new Sphere( center, 0.2, new Lambertian( albedo ) ) );
						}
						else if( matPick < 0.95 )
						{
							// metal
							var albedo = Vector.Random() * 0.5 + new Vector( 0.5, 0.5, 0.5 );
							var fuzz = RandomDouble() * 0.1;
							scene.Add( new Sphere( center, 0.2, new Metal( albedo, fuzz ) ) );
						}
						else
						{
							// glass
							var fuzz = RandomDouble() > 0.85 ? RandomDouble() * 0.05 : 0.0;
							scene.Add( new Sphere( center, 0.2, new Dielectric( 1.5, fuzz ) ) );
						}
					}
				}
			}

			scene.Add( new Sphere( new Vector( 0, 1, 0 ), 1.0, new Dielectric( 1.5, 0.0 ) ) );

			scene.Add( new Sphere( new Vector( -4, 1, 0 ), 1.0, new Lambertian( new Vector( 0.4, 0.2, 0.1 ) ) ) );

			scene.Add( new Sphere( new Vector( 4, 1, 0 ), 1.0, new Metal( new Vector( 0.7, 0.6, 0.5 ), 0.0 ) ) );
			scene.InitializeLookup();

			return new Camera( new Vector( 13, 2, 3 ), new Vector( 0, 0, 0 ), new Vector( 0, 1, 0 ), Width, Height, scene )
			{
				FovInDegrees = 20,
				DefocusAngleInDegrees = 0.6,
				SamplesPerPixel = SamplesPerPixel
			};
		}

		while( true )
		{
			if( System.DateTime.UtcNow - _imageUpdateTimestamp > TimeSpan.FromSeconds( 5 ) )
			{
				image.DumpBitmap = true;
			}
			camera.GenerateImage( image );
		}
	}

	private void OnImageGenerated( object? sender, EventArgs e )
	{
		if( sender is not OutputImage outputImage )
			return;

		_imageUpdateTimestamp = System.DateTime.UtcNow;
		Image = outputImage.Image;
		Samples = outputImage.Divisor;
		PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( Image ) ) );
		PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( nameof( Samples ) ) );
	}
}