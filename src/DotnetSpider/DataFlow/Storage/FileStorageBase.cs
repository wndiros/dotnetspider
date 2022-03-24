using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DotnetSpider.DataFlow.Storage
{
	/// <summary>
	/// File storage for parsing results
	/// </summary>
	public abstract class FileStorageBase : DataFlowBase
	{
		private readonly object _locker = new();

		/// <summary>
		/// root folder for storage
		/// </summary>
		protected string Folder { get; private set; }

		public override Task InitializeAsync()
		{
			Folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "files");
			if (!Directory.Exists(Folder))
			{
				Directory.CreateDirectory(Folder);
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// get storage folder
		/// </summary>
		/// <param name="owner">Task ID</param>
		/// <returns></returns>
		protected string GetDataFolder(string owner)
		{
			var path = Path.Combine(Folder, owner);
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}

			return path;
		}

		/// <summary>
		/// Create a file writer
		/// </summary>
		/// <param name="file"></param>
		protected StreamWriter OpenWrite(string file)
		{
			lock (_locker)
			{
				return new StreamWriter(File.OpenWrite(file), Encoding.UTF8);
			}
		}
	}
}
