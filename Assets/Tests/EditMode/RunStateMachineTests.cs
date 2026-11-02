using System.Linq;
using Alchemy.Core;
using NUnit.Framework;
using static Alchemy.Tests.EditMode.TestContent;

namespace Alchemy.Tests.EditMode
{
    public class RunStateMachineTests
    {
        private static RunStateMachine NewRun(RunDef def = null, ulong seed = 1) =>
            new RunStateMachine(def ?? CreateRun(), seed, checkInvariants: true);

        private static void Ok(RunStateMachine m, ICommand command)
        {
            var result = m.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Error);
        }

        private static void ToPlacing(RunStateMachine m)
        {
            Ok(m, new ContinueCommand());
            Ok(m, new LeaveMarketCommand());
        }

        private static void ToWorkshop(RunStateMachine m)
        {
            Ok(m, new ContinueCommand());
            for (var i = 0; i < 3; i++)
                Ok(m, new SkipBrewCommand());
        }

        private static Product SteelWithStats(int statCount)
        {
            var stats = new[] { new StatLine(StatIds.Quality, 6), new StatLine("sharpness", 2), new StatLine("thorns", 1) };
            return new Product(Steel, 6, 1, stats.Take(statCount).ToArray());
        }

        private static int IndexOf(RunState s, ItemDef def) => s.Bag.ToList().FindIndex(p => p.Def == def);

        [Test]
        public void NewRun_StartsAtSeasonOneDecree_WithStarterKitAndZeroGold()
        {
            var s = NewRun().State;

            Assert.That(s.Season, Is.EqualTo(1));
            Assert.That(s.Phase, Is.EqualTo(RunPhase.Decree));
            Assert.That(s.Gold, Is.EqualTo(0));
            Assert.That(s.Reputation, Is.EqualTo(3));
            Assert.That(s.Bag.Select(p => p.Def), Is.EqualTo(new[] { Iron, Coal, Lens }));
        }

        [Test]
        public void Place_WhenBrewing_Rejects()
        {
            var m = NewRun();
            ToPlacing(m);
            Ok(m, new PlaceCommand(IndexOf(m.State, Iron), 0, 0));
            Ok(m, new BrewCommand());

            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.Brewing));
            Assert.That(m.Execute(new PlaceCommand(0, 0, 0)).Accepted, Is.False);
            Assert.That(m.Execute(new UnplaceCommand(0, 0)).Accepted, Is.False);
            Assert.That(m.Execute(new MoveCommand(0, 0, 0, 1)).Accepted, Is.False);
            Assert.That(m.Execute(new BrewCommand()).Accepted, Is.False);
        }

        [Test]
        public void Brew_ConsumesGridAndAddsProductToBag()
        {
            var m = NewRun();
            ToPlacing(m);
            Ok(m, new PlaceCommand(IndexOf(m.State, Iron), 0, 0));
            Ok(m, new PlaceCommand(IndexOf(m.State, Coal), 0, 2));
            Ok(m, new BrewCommand());

            Assert.That(m.State.Grid.ToFeedSequence(), Is.All.Null);
            Assert.That(m.State.Bag.Select(p => p.Def), Is.EqualTo(new[] { Lens, Steel }));
            Assert.That(m.State.LastBrew.Product.Def, Is.SameAs(Steel));
        }

        [Test]
        public void Unplace_ReturnsItemToBag()
        {
            var m = NewRun();
            ToPlacing(m);
            Ok(m, new PlaceCommand(IndexOf(m.State, Iron), 1, 1));
            Ok(m, new UnplaceCommand(1, 1));

            Assert.That(m.State.Bag.Count, Is.EqualTo(3));
            Assert.That(m.State.Grid[1, 1], Is.Null);
        }

        [Test]
        public void Place_WhenLens_Rejects()
        {
            var m = NewRun();
            ToPlacing(m);

            Assert.That(m.Execute(new PlaceCommand(IndexOf(m.State, Lens), 0, 0)).Accepted, Is.False);
        }

        [Test]
        public void SkipBrew_ReturnsGridItems_CountsTurn_ThirdTurnGoesToWorkshop()
        {
            var m = NewRun();
            ToPlacing(m);
            Ok(m, new PlaceCommand(0, 0, 0));
            Ok(m, new SkipBrewCommand());

            Assert.That(m.State.Bag.Count, Is.EqualTo(3));
            Assert.That(m.State.BrewsUsed, Is.EqualTo(1));
            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.Market));

            Ok(m, new SkipBrewCommand());
            Ok(m, new SkipBrewCommand());
            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.Workshop));
            Assert.That(m.Execute(new SkipBrewCommand()).Accepted, Is.False);
        }

        [Test]
        public void Shop_Buy_WhenBagFull_Rejects()
        {
            var m = NewRun();
            Ok(m, new ContinueCommand());
            m.State.Gold = 1000;
            while (m.State.Bag.Count < m.State.BagCapacity)
                m.State.BagItems.Add(Raw(Iron));

            Assert.That(m.Execute(new BuyCommand(0)).Accepted, Is.False);
            Assert.That(m.State.Gold, Is.EqualTo(1000));
        }

        [Test]
        public void Shop_Buy_WhenNotEnoughGold_Rejects()
        {
            var m = NewRun();
            Ok(m, new ContinueCommand());

            Assert.That(m.Execute(new BuyCommand(0)).Accepted, Is.False);
            Assert.That(m.State.Bag.Count, Is.EqualTo(3));
        }

        [Test]
        public void Shop_Buy_PaysPriceAndAddsItem()
        {
            var m = NewRun();
            Ok(m, new ContinueCommand());
            m.State.Gold = 100;
            var offer = m.State.Offers[0];

            Ok(m, new BuyCommand(0));

            Assert.That(m.State.Gold, Is.EqualTo(100 - offer.Price));
            Assert.That(m.State.Bag.Last().Def, Is.SameAs(offer.Item));
        }

        [Test]
        public void Shop_BuySameOfferTwice_SecondRejected_SlotsKeepPositions()
        {
            var m = NewRun();
            Ok(m, new ContinueCommand());
            m.State.Gold = 100;
            var items = m.State.Offers.Select(o => o.Item).ToArray();

            Ok(m, new BuyCommand(0));
            var gold = m.State.Gold;

            Assert.That(m.Execute(new BuyCommand(0)).Accepted, Is.False);
            Assert.That(m.State.Gold, Is.EqualTo(gold));
            Assert.That(m.State.Offers[0].Sold, Is.True);
            Assert.That(m.State.Offers.Select(o => o.Item), Is.EqualTo(items));
        }

        [Test]
        public void Shop_Reroll_ClearsSoldSlots()
        {
            var m = NewRun();
            Ok(m, new ContinueCommand());
            m.State.Gold = 100;
            Ok(m, new BuyCommand(0));

            Ok(m, new RerollCommand());

            Assert.That(m.State.Offers.Any(o => o.Sold), Is.False);
        }

        [Test]
        public void Shop_SellPrice_IgnoresStats()
        {
            var m = NewRun();
            Ok(m, new ContinueCommand());
            m.State.BagItems.Add(SteelWithStats(1));
            m.State.BagItems.Add(SteelWithStats(3));

            Ok(m, new SellCommand(3));
            var richGain = m.State.Gold;
            Ok(m, new SellCommand(3));

            Assert.That(richGain, Is.GreaterThan(0));
            Assert.That(m.State.Gold, Is.EqualTo(2 * richGain));
        }

        [Test]
        public void Market_NeverOffersTierAboveShopMaxTierOrBrewedItems()
        {
            for (ulong seed = 0; seed < 200; seed++)
            {
                var m = NewRun(seed: seed);
                Ok(m, new ContinueCommand());
                m.State.Gold = 1000;
                for (var r = 0; r < 5; r++)
                {
                    foreach (var o in m.State.Offers)
                    {
                        Assert.That(o.Item.Tier, Is.LessThanOrEqualTo(3), $"seed {seed}");
                        Assert.That(o.Item.Recipe, Is.Null, $"seed {seed}");
                    }

                    Ok(m, new RerollCommand());
                }
            }
        }

        [Test]
        public void Reroll_CostGrowsByStep_ResetsNextTurn()
        {
            var m = NewRun();
            Ok(m, new ContinueCommand());
            m.State.Gold = 100;

            Ok(m, new RerollCommand());
            Ok(m, new RerollCommand());
            Assert.That(m.State.Gold, Is.EqualTo(100 - 2 - 3));

            Ok(m, new SkipBrewCommand());
            Assert.That(Market.RerollCost(m.State), Is.EqualTo(2));
        }

        [Test]
        public void Modifier_FreeReroll_CostsNothingFirst()
        {
            var m = NewRun();
            Ok(m, new ContinueCommand());
            m.State.Modifier = new SeasonModifierDef("free", "Chợ phiên", "", freeRerolls: 1);

            Assert.That(Market.RerollCost(m.State), Is.EqualTo(0));
            Ok(m, new RerollCommand());
            Assert.That(Market.RerollCost(m.State), Is.EqualTo(2));
        }

        [Test]
        public void Modifier_BuyPrice_ScalesPrice()
        {
            var s = NewRun().State;
            var basePrice = Market.BuyPrice(s, Steel);
            s.Modifier = new SeasonModifierDef("x", "x", "", new[] { new ParamScale(ScaledParam.BuyPrice, 200) });

            Assert.That(Market.BuyPrice(s, Steel), Is.EqualTo(basePrice * 2).Within(1));
        }

        [Test]
        public void Inspect_WhenLensSmallerThanStats_RevealsRandomSubset()
        {
            var m = NewRun();
            var steel = SteelWithStats(3);
            m.State.BagItems.Add(steel);
            m.State.BagItems.Add(Raw(Lens));

            Ok(m, new InspectCommand(IndexOf(m.State, Lens), IndexOf(m.State, Steel)));
            Assert.That(steel.RevealedStats.Count, Is.EqualTo(1));
            Ok(m, new InspectCommand(IndexOf(m.State, Lens), IndexOf(m.State, Steel)));

            Assert.That(steel.RevealedStats.Count, Is.EqualTo(2));
            Assert.That(steel.RevealedStats.Select(x => x.StatId).Distinct().Count(), Is.EqualTo(2));
            Assert.That(m.State.Bag.Count(p => p.Def == Lens), Is.EqualTo(0));
        }

        [Test]
        public void Inspect_WhenLensCoversAllLines_RevealsAll()
        {
            var m = NewRun();
            var steel = SteelWithStats(1);
            m.State.BagItems.Add(steel);

            Ok(m, new InspectCommand(IndexOf(m.State, Lens), IndexOf(m.State, Steel)));

            Assert.That(steel.RevealedStats.Count, Is.EqualTo(1));
            Assert.That(steel.CanInspect, Is.False);
        }

        [Test]
        public void Inspect_WhenAllRevealedOrNotBrewed_RejectsAndKeepsLens()
        {
            var m = NewRun();
            var steel = SteelWithStats(1);
            m.State.BagItems.Add(steel);
            m.State.BagItems.Add(Raw(Lens));
            Ok(m, new InspectCommand(IndexOf(m.State, Lens), IndexOf(m.State, Steel)));

            Assert.That(m.Execute(new InspectCommand(IndexOf(m.State, Lens), IndexOf(m.State, Steel))).Accepted, Is.False);
            Assert.That(m.Execute(new InspectCommand(IndexOf(m.State, Lens), IndexOf(m.State, Iron))).Accepted, Is.False);
            Assert.That(m.State.Bag.Count(p => p.Def == Lens), Is.EqualTo(1));
        }

        [Test]
        public void Inspect_WhenBrewing_Rejects()
        {
            var m = NewRun();
            m.State.BagItems.Add(SteelWithStats(3));
            ToPlacing(m);
            Ok(m, new PlaceCommand(IndexOf(m.State, Iron), 0, 0));
            Ok(m, new BrewCommand());

            Assert.That(m.Execute(new InspectCommand(IndexOf(m.State, Lens), IndexOf(m.State, Steel))).Accepted, Is.False);
        }

        [Test]
        public void Offering_Always_ConsumesProduct()
        {
            var m = NewRun(CreateRun(opponentPower: 1000));
            m.State.BagItems.Add(SteelWithStats(1));
            ToWorkshop(m);
            Ok(m, new ContinueCommand());
            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.Offering));

            Ok(m, new OfferCommand(IndexOf(m.State, Steel)));

            Assert.That(IndexOf(m.State, Steel), Is.EqualTo(-1));
            Assert.That(m.State.LastBattle.Won, Is.False);
        }

        [Test]
        public void Offering_WhenNotBrewed_Rejects()
        {
            var m = NewRun();
            m.State.BagItems.Add(SteelWithStats(1));
            ToWorkshop(m);
            Ok(m, new ContinueCommand());

            Assert.That(m.Execute(new OfferCommand(IndexOf(m.State, Iron))).Accepted, Is.False);
        }

        [Test]
        public void Battle_WhenPowerEqualsOpponent_Wins()
        {
            var m = NewRun(CreateRun(opponentPower: 6));
            m.State.BagItems.Add(SteelWithStats(1));
            ToWorkshop(m);
            Ok(m, new ContinueCommand());
            Ok(m, new OfferCommand(IndexOf(m.State, Steel)));

            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.Battle));
            Assert.That(m.State.LastBattle.Won, Is.True);
            Assert.That(m.State.Reputation, Is.EqualTo(3));
        }

        [Test]
        public void Battle_WhenPowerBelowOpponent_LosesReputation()
        {
            var m = NewRun(CreateRun(opponentPower: 6.01));
            m.State.BagItems.Add(SteelWithStats(1));
            ToWorkshop(m);
            Ok(m, new ContinueCommand());
            Ok(m, new OfferCommand(IndexOf(m.State, Steel)));

            Assert.That(m.State.LastBattle.Won, Is.False);
            Assert.That(m.State.Reputation, Is.EqualTo(2));
        }

        [Test]
        public void Battle_WhenNothingToOffer_AutoLoses()
        {
            var m = NewRun();
            ToWorkshop(m);
            Ok(m, new ContinueCommand());

            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.Battle));
            Assert.That(m.State.LastBattle.Won, Is.False);
            Assert.That(m.State.LastBattle.Equipment, Is.Null);
            Assert.That(m.State.Reputation, Is.EqualTo(2));
        }

        [Test]
        public void Reputation_WhenZero_EndsRunExiled()
        {
            var m = NewRun();
            for (var season = 1; season <= 3; season++)
            {
                ToWorkshop(m);
                Ok(m, new ContinueCommand());
                Ok(m, new ContinueCommand());
            }

            Assert.That(m.State.Reputation, Is.EqualTo(0));
            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.RunEnd));
            Assert.That(m.State.Outcome, Is.EqualTo(RunOutcome.Exiled));
            Assert.That(m.Execute(new ContinueCommand()).Accepted, Is.False);
        }

        [Test]
        public void Run_AfterFinalSeasonWithReputation_EndsWon_EvenIfLastBattleLost()
        {
            var m = NewRun();
            for (var season = 1; season <= 8; season++)
            {
                if (season < 7)
                    m.State.BagItems.Add(SteelWithStats(1));
                ToWorkshop(m);
                Ok(m, new ContinueCommand());
                if (m.State.Phase == RunPhase.Offering)
                    Ok(m, new OfferCommand(IndexOf(m.State, Steel)));
                Ok(m, new ContinueCommand());
                if (m.State.Phase == RunPhase.Spoils)
                    Ok(m, new ChooseRewardCommand(0));
            }

            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.RunEnd));
            Assert.That(m.State.Outcome, Is.EqualTo(RunOutcome.Won));
            Assert.That(m.State.Reputation, Is.EqualTo(1));
            Assert.That(m.State.Wins, Is.EqualTo(6));
        }

        [Test]
        public void Spoils_AfterWin_OffersDistinctChoices_ChoosingAppliesAndStartsNextSeason()
        {
            var m = NewRun();
            m.State.BagItems.Add(SteelWithStats(1));
            ToWorkshop(m);
            Ok(m, new ContinueCommand());
            Ok(m, new OfferCommand(IndexOf(m.State, Steel)));
            Ok(m, new ContinueCommand());

            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.Spoils));
            Assert.That(m.State.RewardChoices.Count, Is.InRange(1, 3));
            Assert.That(m.State.RewardChoices.Distinct().Count(), Is.EqualTo(m.State.RewardChoices.Count));

            var bag = m.State.BagCapacity;
            var bagIndex = m.State.RewardChoices.ToList().FindIndex(r => r.Kind == RewardKind.BagExpand);
            Ok(m, new ChooseRewardCommand(bagIndex));

            Assert.That(m.State.BagCapacity, Is.EqualTo(bag + 2));
            Assert.That(m.State.Season, Is.EqualTo(2));
            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.Decree));
        }

        [Test]
        public void Battle_AfterLoss_NoSpoils()
        {
            var m = NewRun();
            ToWorkshop(m);
            Ok(m, new ContinueCommand());
            Ok(m, new ContinueCommand());

            Assert.That(m.State.Phase, Is.EqualTo(RunPhase.Decree));
            Assert.That(m.State.Season, Is.EqualTo(2));
        }
    }
}
