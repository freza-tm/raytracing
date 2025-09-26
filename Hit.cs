internal readonly record struct Hit( Vector Location, Vector Normal, bool FrontFace, double T, IMaterial Material )
{
	public static Hit NoHit = new(
		Vector.Invalid(),
		Vector.Invalid(),
		true,
		double.NaN,
		new Lambertian( Vector.Invalid() )
	 );

}