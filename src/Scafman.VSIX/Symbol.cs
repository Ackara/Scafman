using System;

namespace Acklann.Scafman
{
	public static class Symbol
	{
		public const string Name = "Scanman";
		
		public const string Version = "0.0.1";

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
			public const int ActionCommandGroup = 0x103;
			public const int MiscCommandGroup = 0x104;
			public const int MainMenuId = 0x201;
			public const int CurrentLevelCommandId = 0x300;
			public const int GotoConfigurationPageCommandId = 0x303;
			public const int OpenTemplateDirectoryCommandId = 0x330;
			public const int CompareActiveDocumentWithTemplateCommandId = 0x331;
			public const int OpenItemGroupConfigurationFileCommandId = 0x333;
		}
	}
}
