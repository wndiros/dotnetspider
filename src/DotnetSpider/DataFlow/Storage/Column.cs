using System.Reflection;

namespace DotnetSpider.DataFlow.Storage
{
	/// <summary>
	/// 列信息
	/// </summary>
	public class Column
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public int Length { get; set; } = 255;
		public bool Required { get; set; }

		/// <summary>
		/// Property reflection, used to set the analytic value to the entity object
		/// </summary>
		public PropertyInfo PropertyInfo { get; set; }
	}
}
