internal record struct Pixel( byte Red, byte Green, byte Blue )
{
	public Pixel( Vector v )
	: this( v.X, v.Y, v.Z ) { }

	public Pixel( double red, double green, double blue )
	: this( ClampToByte( red ), ClampToByte( green ), ClampToByte( blue ) ) { }

	private static byte ClampToByte( double d )
	{
		if( d <= 0.0 )
			return 0;

		return (byte) (Math.Clamp( Math.Sqrt( d ), 0.0, 1.0 ) * (256.0 - 1e-8));
	}
}