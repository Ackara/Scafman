using System;

namespace Acklann.Powerbar
{
	public static class Symbol
	{
		public struct Package
		{
			public const string GuidString = "3c078121-2119-42af-9055-50abb4af5afe";
			public static readonly Guid Guid = new Guid("3c078121-2119-42af-9055-50abb4af5afe");
		}
		public struct CmdSet
		{
			public const string GuidString = "069838a4-a734-4fed-bc02-ef643489a6e7";
			public static readonly Guid Guid = new Guid("069838a4-a734-4fed-bc02-ef643489a6e7");
			public const int VSToolsMenuGroup = 0x1020;
			public const int FileContextMenuGroup = 0x101;
			public const int ProjectContextMenuGroup = 0x102;
			public const int ToolsMenuId = 0x201;
			public const int InvokeCommandId = 0x300;
		}
	}
}
