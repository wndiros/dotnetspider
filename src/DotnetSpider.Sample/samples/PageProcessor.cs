namespace DotnetSpider.Sample.samples
{
	internal class PageProcessor
	{
		public System.Func<object, object> ExtractLinks { get; set; }
		public System.Func<object, object, string[]> HandleLinks { get; set; }
	}
}