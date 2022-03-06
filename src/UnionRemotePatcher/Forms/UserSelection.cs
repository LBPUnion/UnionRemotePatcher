using Eto.Forms;
using Eto.Drawing;

namespace UnionRemotePatcher
{
	partial class UserSelection : Form
	{
		public ListBox AccountsList = new ListBox();
		public string GameID = "";
		public string SelectedUserID = "";

		public UserSelection(string[] items, string gameID)
		{
			foreach (string item in items)
			{
				AccountsList.Items.Add(item);
			}

			GameID = gameID;

			InitializeComponent();
		}
		void InitializeComponent()
		{
			Title = "Select User";
			MinimumSize = new Size(300, 200);
			Resizable = false;
			Padding = 10;

			AccountsList.Size = new Size(this.MinimumSize.Width, this.MinimumSize.Height);

			Button confirmBtn = new Button()
			{
				Size = new Size(this.MinimumSize.Width, Size.Height),
				Text = "Continue",
			};

			Content = new StackLayout
			{
				Items =
				{
					$"UnionRemotePatcher needs to know which Local User\non your PS3 owns the PSN license for {GameID}.\n\nPlease select the name of the user that purchased the content and press Continue.",
					// add more controls here
					new StackLayoutItem(new Panel() { Size = new Size(0, 10)}),
					new StackLayoutItem(AccountsList),
					new StackLayoutItem(new Panel() { Size = new Size(0, 10)}),
					new StackLayoutItem(confirmBtn),
				}
			};
		}
	}
}