namespace Alchemy.Core
{
    /// <summary>
    /// Input duy nhất Presentation gửi vào Core. Chỉ là data (để lưu actions[] và replay); luật nằm ở
    /// <see cref="RunStateMachine"/>.
    /// </summary>
    public interface ICommand
    {
    }

    /// <summary>Qua bước kế: Chiếu chỉ → Chợ, sau lượt nung, Xưởng → Dâng, sau trận.</summary>
    public sealed class ContinueCommand : ICommand
    {
    }

    public sealed class BuyCommand : ICommand
    {
        public int OfferIndex { get; }
        public BuyCommand(int offerIndex) => OfferIndex = offerIndex;
    }

    public sealed class SellCommand : ICommand
    {
        public int BagIndex { get; }
        public SellCommand(int bagIndex) => BagIndex = bagIndex;
    }

    public sealed class RerollCommand : ICommand
    {
    }

    /// <summary>Rời Chợ, sang đặt nguyên liệu lên lưới.</summary>
    public sealed class LeaveMarketCommand : ICommand
    {
    }

    public sealed class PlaceCommand : ICommand
    {
        public int BagIndex { get; }
        public int Row { get; }
        public int Col { get; }

        public PlaceCommand(int bagIndex, int row, int col)
        {
            BagIndex = bagIndex;
            Row = row;
            Col = col;
        }
    }

    public sealed class UnplaceCommand : ICommand
    {
        public int Row { get; }
        public int Col { get; }

        public UnplaceCommand(int row, int col)
        {
            Row = row;
            Col = col;
        }
    }

    /// <summary>Di chuyển sang ô khác; ô đích có vật phẩm thì đổi chỗ.</summary>
    public sealed class MoveCommand : ICommand
    {
        public int FromRow { get; }
        public int FromCol { get; }
        public int ToRow { get; }
        public int ToCol { get; }

        public MoveCommand(int fromRow, int fromCol, int toRow, int toCol)
        {
            FromRow = fromRow;
            FromCol = fromCol;
            ToRow = toRow;
            ToCol = toCol;
        }
    }

    /// <summary>Nung: khóa lưới, tiêu hao mọi thứ trên lưới.</summary>
    public sealed class BrewCommand : ICommand
    {
    }

    public sealed class SkipBrewCommand : ICommand
    {
    }

    public sealed class InspectCommand : ICommand
    {
        public int LensIndex { get; }
        public int TargetIndex { get; }

        public InspectCommand(int lensIndex, int targetIndex)
        {
            LensIndex = lensIndex;
            TargetIndex = targetIndex;
        }
    }

    public sealed class OfferCommand : ICommand
    {
        public int BagIndex { get; }
        public OfferCommand(int bagIndex) => BagIndex = bagIndex;
    }

    public sealed class ChooseRewardCommand : ICommand
    {
        public int Index { get; }
        public ChooseRewardCommand(int index) => Index = index;
    }

    public sealed class CommandResult
    {
        public static readonly CommandResult Ok = new CommandResult(true, null);

        public bool Accepted { get; }

        /// <summary>Lý do từ chối cho UI. Không bao giờ chứa thông tin công thức hay số liệu ẩn.</summary>
        public string Error { get; }

        private CommandResult(bool accepted, string error)
        {
            Accepted = accepted;
            Error = error;
        }

        public static CommandResult Reject(string error) => new CommandResult(false, error);
    }
}
