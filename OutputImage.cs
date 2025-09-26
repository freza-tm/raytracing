using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

internal sealed class OutputImage : IDisposable
{
	private Image<Rgba32> _bitmap;

	public OutputImage( int width, int height )
	{
		_bitmap = new Image<Rgba32>( width, height );
	}

	public void Dispose() => _bitmap.Dispose();

	public void SaveToFile( string fileName )
	{
		_bitmap.Save( fileName );
	}

	public void SetPixels( Func<int, IEnumerable<Pixel>> pixelsProducer )
	{
		int doneRows = 0;
		List<Pixel>[] generatedImage = new List<Pixel>[_bitmap.Height];

		Parallel.ForEach( Enumerable.Range( 0, _bitmap.Height ), GenerateRow );

		void GenerateRow( int row )
		{
			var buffer = new List<Pixel>( _bitmap.Width );
			buffer.AddRange( pixelsProducer( row ) );

			var progress = Interlocked.Increment( ref doneRows );
			System.Console.Out.WriteLine( $"Scanline {progress + 1,4} / {_bitmap.Height,4}" );

			generatedImage[row] = buffer;
		}

		_bitmap.ProcessPixelRows( StorePixels );

		void StorePixels( PixelAccessor<Rgba32> pixelAcessor )
		{
			for( int row = 0; row < _bitmap.Height; row++ )
			{
				var rowData = pixelAcessor.GetRowSpan( row );
				var source = generatedImage[row];
				for( int column = 0; column < pixelAcessor.Width; column++ )
				{
					rowData[column].R = source[column].Red;
					rowData[column].G = source[column].Green;
					rowData[column].B = source[column].Blue;
				}
			}
		}
	}
}