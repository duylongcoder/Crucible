using System.Collections.Generic;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Presentation
{
    public class SpoilsView : GameView
    {
        [SerializeField] private ButtonList _choices;

        protected override void CollectViewReferences(List<(string field, Object value)> references)
        {
            references.Add((nameof(_choices), _choices));
        }

        protected override bool IsVisible(RunPhase phase) => phase == RunPhase.Spoils;

        protected override void Render(RunState s)
        {
            var rewards = s.RewardChoices;
            _choices.Show(rewards.Count, (b, i) =>
                b.Bind(i, UiText.Reward(rewards[i]), UiText.Slot, index => Game.Send(new ChooseRewardCommand(index))));
        }
    }
}
