#region References

using System;

#endregion

namespace Scribe.Models.Data
{
	[Flags]
	public enum PageUpdateType
	{
		Pending = 0x01,
		Approve = 0x02,
		Reject = 0x04,
		Publish = 0x08,
		Unpublish = 0x10,
		SetHomepage = 0x20
	}
}