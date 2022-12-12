using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

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

        [SerializeField] private Slider _apSlider;
        [SerializeField] private Slider _mlSlider;
        [SerializeField] private Slider _rSlider;

        //5.4f, 5.739f, 0.332f
        // start the craniotomy at bregma
        private Vector3 position = Vector3.zero;
        private float size = 0f;

        private int _lastCraniotomyIdx = 0;

        [FormerlySerializedAs("craniotomySkull")] [SerializeField] private CraniotomySkull _craniotomySkull;

        private void Start()
        {
            // Start with craniotomy disabled
            UpdateSize(1);
            UpdateCraniotomy();
        }

        private void OnEnable()
        {
            _craniotomySkull.Enable();
            UpdateCraniotomyIdx(_lastCraniotomyIdx);
        }

        private void OnDisable()
        {
            _craniotomySkull.Disable();
        }

        public void UpdateAP(float ap)
        {
            position.y = -ap;
            UpdateCraniotomy();
            UpdateText();
        }
        public void UpdateML(float ml)
        {
            position.x = -ml;
            UpdateCraniotomy();
            UpdateText();
        }
        public void UpdateSize(float newSize)
        {
            size = newSize;
            UpdateCraniotomy();
            UpdateText();
        }

        public void UpdateCraniotomyIdx(int craniotomyIdx)
        {
            _lastCraniotomyIdx = craniotomyIdx;
            _craniotomySkull.SetActiveCraniotomy(craniotomyIdx);
            position = _craniotomySkull.GetCraniotomyPosition();
            size = _craniotomySkull.GetCraniotomySize();
            UpdateText();
            UpdateSliders();
        }

        private void UpdateSliders()
        {
            _apSlider.value = position.x;
            _mlSlider.value = position.y;
            _rSlider.value = size;
        }

        private void UpdateText()
        {
            _apText.text = "AP: " + Mathf.RoundToInt(-position.y * 1000f);
            _mlText.text = "ML: " + Mathf.RoundToInt(-position.x * 1000f);
            _rText.text = "r: " + Mathf.RoundToInt(size * 1000f);
        }

        private void UpdateCraniotomy()
        {
            // We need to rotate the x/y coordinates into the current transformed space... TODO

            if (_craniotomySkull != null)
            {
                _craniotomySkull.SetCraniotomyPosition(position);
                _craniotomySkull.SetCraniotomySize(size);
            }
        }
    }

}