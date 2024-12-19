namespace Game.UI
{
    public class BuyPackDialogModel : ConfirmationDialogModel
    {
        public readonly string IconUrl;
        public readonly string IconKey;
        public readonly string Title;
        public readonly string TitleIconKey;
        public readonly TextWithIconModel[] Costs;

        public BuyPackDialogModel(
            string iconUrl,
            string iconKey,
            string title,
            string titleIconKey,
            TextWithIconModel[] costs,
            string confirmText = null,
            string declineText = null) : base(confirmText, declineText)
        {
            IconUrl = iconUrl;
            IconKey = iconKey;
            Title = title;
            TitleIconKey = titleIconKey;
            Costs = costs;
        }
    }
}