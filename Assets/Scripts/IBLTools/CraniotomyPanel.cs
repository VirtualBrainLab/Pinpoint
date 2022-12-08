using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using TrajectoryPlanner;
using UnityEngine.Serialization;

namespace IBLTools
{

    public class CraniotomyPanel : MonoBehaviour
    {
        [FormerlySerializedAs("apText")] [SerializeField]
        private TMP_Text _apText;
        [FormerlySerializedAs("mlText")] [SerializeField]
        private TMP_Text _mlText;
        [FormerlySerializedAs("rText")] [SerializeField]
        private TMP_Text _rText;

        [FormerlySerializedAs("tpmanager")] [SerializeField]
        private TrajectoryPlannerManager _tpmanager;

        //5.4f, 5.739f, 0.332f
        // start the craniotomy at bregma
        private Vector3 position = Vector3.zero;

        private float size = 1f;
        private float disabledSize = -1f;

        [FormerlySerializedAs("craniotomySkull")] [SerializeField] private TP_CraniotomySkull _craniotomySkull;

        private void Start()
        {
            // Start with craniotomy disabled
            UpdateSize(1);
            UpdateCraniotomy();
        }

        public void OnDisable()
        {
            disabledSize = size;
            UpdateSize(0);
        }

        public void OnEnable()
        {
            if (disabledSize >= 0f)
                UpdateSize(disabledSize);
            else
                UpdateSize(size);
        }

        public void UpdateAP(float ap)
        {
            position.x = -ap;
            UpdateCraniotomy();
            UpdateText();
        }
        public void UpdateML(float ml)
        {
            position.y = ml;
            UpdateCraniotomy();
            UpdateText();
        }
        public void UpdateSize(float newSize)
        {
            size = newSize;
            UpdateCraniotomy();
            UpdateText();
        }

        private void UpdateText()
        {
            _apText.text = "AP: " + Mathf.RoundToInt(-position.x * 1000f);
            _mlText.text = "ML: " + Mathf.RoundToInt(position.y * 1000f);
            _rText.text = "r: " + Mathf.RoundToInt(size * 1000f);
        }

        private void UpdateCraniotomy()
        {
            if (_craniotomySkull != null)
            {
                _craniotomySkull.SetCraniotomyPosition(position);
                _craniotomySkull.SetCraniotomySize(size);
            }
        }
    }

}