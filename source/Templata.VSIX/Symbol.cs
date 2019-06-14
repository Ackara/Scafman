using System;

namespace Acklann.Templata
{
	public static class Symbol
	{
		public class Package
		{
			public const string GuidString = "3c078121-2119-42af-9055-50abb4af5afe";
			public static readonly Guid Guid = new Guid("3c078121-2119-42af-9055-50abb4af5afe");
		}
		public class CmdSet
		{
			public const string GuidString = "069838a4-a734-4fed-bc02-ef643489a6e7";
			public static readonly Guid Guid = new Guid("069838a4-a734-4fed-bc02-ef643489a6e7");
			public const int VSToolsMenuGroup = 0x1020;
			public const int FileContextMenuGroup = 0x101;
			public const int ProjectContextMenuGroup = 0x102;
			public const int ActionCommandGroup = 0x103;
			public const int MiscCommandGroup = 0x104;
			public const int ToolsMenuId = 0x201;
			public const int CurrentLevelCommandId = 0x300;
			public const int ProjectLevelCommandId = 0x301;
			public const int SolutionLevelCommandId = 0x302;
			public const int ConfigurationPageCommandId = 0x303;
			public const int OpenTemplateDirectoryCommandId = 0x304;
			public const int CompareFileWithTemplateCommandId = 0x305;
			public const int AddFileToTemplateDirectoryCommandId = 0x306;
			public const int OpenItemGroupConfigurationFileCommandId = 0x307;
		}
	}
}
