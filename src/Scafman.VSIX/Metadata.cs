using System;

namespace Acklann.Scafman
{
	public static class Metadata
	{
		public const string Name = "Scafman";
		
		public const string Version = "0.0.36";

		public class Package
		{
			public const string GuidString = "a153dcb8-6275-4a93-988f-2b62da301f36";
			public static readonly Guid Guid = new Guid("a153dcb8-6275-4a93-988f-2b62da301f36");
		}
		public class CmdSet
		{
			public const string GuidString = "1927fb07-3e0c-4213-8cc1-f87f1df0ab42";
			public static readonly Guid Guid = new Guid("1927fb07-3e0c-4213-8cc1-f87f1df0ab42");
			public const int VSMenuGroup = 0x1020;
			public const int MainCommadGroup = 0x103;
			public const int MiscCommandGroup = 0x104;
			public const int ManagementCommandGroup = 0x105;
			public const int MainMenuId = 0x201;
			public const int AddNewItemCommandId = 0x300;
			public const int CompareActiveDocumentWithTemplateCommandId = 0x331;
			public const int ExportActiveDocumentAsTemplateCommandId = 0x0332;
			public const int GotoConfigurationPageCommandId = 0x303;
			public const int OpenTemplateDirectoryCommandId = 0x330;
			public const int OpenItemGroupConfigurationFileCommandId = 0x333;
		}
	}
}
