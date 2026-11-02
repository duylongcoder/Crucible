using System.Collections.Generic;
using Alchemy.Core;
using UnityEngine;

namespace Alchemy.Presentation
{
    /// <summary>View của một phần màn hình; <see cref="GameController"/> gọi <see cref="Refresh"/> sau mỗi Command.</summary>
    public abstract class GameView : ViewBehaviour
    {
        [SerializeField] protected GameController Game;

        protected sealed override void CollectRequiredReferences(List<(string field, Object value)> references)
        {
            references.Add((nameof(Game), Game));
            CollectViewReferences(references);
        }

        protected abstract void CollectViewReferences(List<(string field, Object value)> references);

        /// <summary>Panel tự bật/tắt theo phase; view ẩn thì không cần dựng nội dung.</summary>
        public void Refresh(RunState state)
        {
            var visible = IsVisible(state.Phase);
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
            if (visible && enabled)
                Render(state);
        }

        protected abstract bool IsVisible(RunPhase phase);

        protected abstract void Render(RunState state);
    }
}
