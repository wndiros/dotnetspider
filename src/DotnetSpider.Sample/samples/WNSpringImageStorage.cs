using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotnetSpider.DataFlow;
using Microsoft.Extensions.Logging;

namespace DotnetSpider.Sample.samples
{

	
	internal class WNSpringImageStorage : ImageStorage
	{
		private readonly DependenceServices _services;
		private HashSet<string> _imageExtensions;
		private static readonly object _locker = new();
		private readonly string imagedirectory = "c:\\imagedata\\";
		


		public override Task InitializeAsync()
		{
			base.InitializeAsync();

			_imageExtensions = new HashSet<string>(ImageExtensions);

			return Task.CompletedTask;
		}
		public override Task HandleAsync(DataFlowContext context)
		{
			var _context = context;
			if (IsNullOrEmpty(context))
			{
				Logger.LogWarning("Dataflow context does not contain parsing results");
				return Task.CompletedTask;
			}

			var fileName = context.Request.RequestUri.AbsolutePath;
			if (!_imageExtensions.Any(x => fileName.EndsWith(x)))
			{
				return Task.CompletedTask;
			}

			var path = Path.Combine(imagedirectory, "");
			fileName = Path.GetFileName(fileName);
			path = $"{path}{fileName}";
			var folder = Path.GetDirectoryName(path);
			if (string.IsNullOrWhiteSpace(folder))
			{
				return Task.CompletedTask;
			}

			lock (_locker)
			{
				if (!Directory.Exists(folder))
				{
					Directory.CreateDirectory(folder);
				}
			}

			

			File.WriteAllBytes(path, context.Response.Content.Bytes);

			return Task.CompletedTask;
		}
	}
}
