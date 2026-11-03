using UnityEngine;

namespace Study3D.OutPost
{
    /// <summary>
    /// 선택창에서 고른 캐릭터를 다음 씬까지 들고 가는 저장소.
    /// static 이라 씬이 바뀌어도 값이 남는다. (게임을 껐다 켜면 사라짐)
    /// </summary>
    public static class SelectedCharacter
    {
        public static CharacterData Current { get; private set; }
        public static bool HasValue => Current != null;

        public static void Set(CharacterData data) => Current = data;
        public static void Clear() => Current = null;

        // 에디터에서 Domain Reload 를 꺼둔 경우에도 플레이할 때마다 비워준다
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Current = null;
    }
}
