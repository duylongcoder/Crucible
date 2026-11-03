using Alchemy.Core;

namespace Alchemy.Persistence
{
    /// <summary>ICommand ↔ <see cref="CommandData"/> cho actions[] của file save.</summary>
    internal static class CommandCodec
    {
        public static CommandData Encode(ICommand command)
        {
            switch (command)
            {
                case ContinueCommand _: return Data("continue");
                case BuyCommand c: return Data("buy", c.OfferIndex);
                case SellCommand c: return Data("sell", c.BagIndex);
                case RerollCommand _: return Data("reroll");
                case LeaveMarketCommand _: return Data("leaveMarket");
                case PlaceCommand c: return Data("place", c.BagIndex, c.Row, c.Col);
                case UnplaceCommand c: return Data("unplace", c.Row, c.Col);
                case MoveCommand c: return Data("move", c.FromRow, c.FromCol, c.ToRow, c.ToCol);
                case BrewCommand _: return Data("brew");
                case SkipBrewCommand _: return Data("skipBrew");
                case InspectCommand c: return Data("inspect", c.LensIndex, c.TargetIndex);
                case OfferCommand c: return Data("offer", c.BagIndex);
                case ChooseRewardCommand c: return Data("chooseReward", c.Index);
                default: throw new System.ArgumentException($"Command chưa có mã lưu: {command?.GetType().Name}");
            }
        }

        /// <summary>Null nếu loại không nhận ra (file hỏng hoặc từ phiên bản khác).</summary>
        public static ICommand Decode(CommandData d)
        {
            switch (d?.type)
            {
                case "continue": return new ContinueCommand();
                case "buy": return new BuyCommand(d.a);
                case "sell": return new SellCommand(d.a);
                case "reroll": return new RerollCommand();
                case "leaveMarket": return new LeaveMarketCommand();
                case "place": return new PlaceCommand(d.a, d.b, d.c);
                case "unplace": return new UnplaceCommand(d.a, d.b);
                case "move": return new MoveCommand(d.a, d.b, d.c, d.d);
                case "brew": return new BrewCommand();
                case "skipBrew": return new SkipBrewCommand();
                case "inspect": return new InspectCommand(d.a, d.b);
                case "offer": return new OfferCommand(d.a);
                case "chooseReward": return new ChooseRewardCommand(d.a);
                default: return null;
            }
        }

        private static CommandData Data(string type, int a = 0, int b = 0, int c = 0, int d = 0) =>
            new CommandData { type = type, a = a, b = b, c = c, d = d };
    }
}
