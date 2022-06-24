using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotnetSpider.DataFlow;

namespace DotnetSpider.Sample.samples
{
	public interface IDataFlowWN : IDataFlow
	{
		Task RecordExists(DataFlowContext context, IDictionary<Type, ICollection<dynamic>> entities);
	}
}
