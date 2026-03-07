using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;

namespace UI.KillLog
{
    /// <summary>
    /// 몬스터 처치 시 유닛별 데미지 기여도를 표시하는 팝업 패널.
    /// GameScene의 Canvas에 배치하고 Inspector에서 참조를 연결해야 한다.
    /// </summary>
    public class KillLogPanel : SceneSingleton<KillLogPanel>
    {
        // ASCII 제어 문자 구분자 — Monster.SerializeKillLog와 반드시 동일해야 한다
        private const char MonsterSep = '\u0001';
        private const char EntrySep = '\u0002';
        private const char KvSep = '\u0003';

        [Header("패널 구성")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI monsterNameText;
        [SerializeField] private Transform entriesParent;
        [SerializeField] private KillLogEntryUI entryPrefab;

        [Header("표시 설정")]
        [SerializeField] private float displayDuration = 4f;

        private readonly List<KillLogEntryUI> _entryPool = new();
        private Coroutine _hideCoroutine;
        private WaitForSeconds _waitForHide;

        // GC 없는 정렬을 위해 delegate를 static으로 캐시한다
        private static readonly Comparison<(string name, int damage)> s_sortByDamageDesc =
            (a, b) => b.damage.CompareTo(a.damage);

        protected override void Awake()
        {
            base.Awake();
            panel?.SetActive(false);
            _waitForHide = new WaitForSeconds(displayDuration);
        }

        /// <summary>
        /// 직렬화된 킬 로그 문자열을 파싱해 패널을 표시한다.
        /// Monster.SerializeKillLog가 생성한 포맷을 입력으로 받는다.
        /// </summary>
        public void ShowLog(string logPayload)
        {
            if (string.IsNullOrEmpty(logPayload)) return;

            int sepIdx = logPayload.IndexOf(MonsterSep);
            if (sepIdx < 0) return;

            string monsterName = logPayload[..sepIdx];
            string entriesStr = logPayload[(sepIdx + 1)..];

            var parsed = ParseEntries(entriesStr);
            if (parsed.Count == 0) return;

            parsed.Sort(s_sortByDamageDesc);

            int total = 0;
            foreach (var e in parsed) total += e.damage;

            RefreshUI(monsterName, parsed, total);

            if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
            _hideCoroutine = StartCoroutine(HideAfterDelay());
        }

        private void RefreshUI(string monsterName, List<(string name, int damage)> entries, int total)
        {
            if (monsterNameText) monsterNameText.text = monsterName;

            // 풀에 있는 항목 재사용, 부족하면 생성
            for (int i = _entryPool.Count; i < entries.Count; i++)
                _entryPool.Add(Instantiate(entryPrefab, entriesParent));

            for (int i = 0; i < _entryPool.Count; i++)
            {
                bool active = i < entries.Count;
                _entryPool[i].gameObject.SetActive(active);
                if (active)
                    _entryPool[i].SetData(entries[i].name, entries[i].damage, total);
            }

            panel?.SetActive(true);
        }

        private IEnumerator HideAfterDelay()
        {
            yield return _waitForHide;
            panel?.SetActive(false);
        }

        private static List<(string name, int damage)> ParseEntries(string raw)
        {
            var result = new List<(string, int)>();
            if (string.IsNullOrEmpty(raw)) return result;

            string[] parts = raw.Split(EntrySep);
            foreach (string part in parts)
            {
                int kv = part.IndexOf(KvSep);
                if (kv < 0) continue;
                if (int.TryParse(part[(kv + 1)..], out int dmg))
                    result.Add((part[..kv], dmg));
            }
            return result;
        }
    }

}
