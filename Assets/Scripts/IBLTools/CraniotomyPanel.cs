using TMPro;
using UnityEngine;
using TrajectoryPlanner;

namespace IBLTools
{

    public class CraniotomyPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _apText;
        [SerializeField] private TMP_Text _mlText;
        [SerializeField] private TMP_Text _radiusText;

        [SerializeField] TrajectoryPlannerManager _TPManager;

        //5.4f, 5.739f, 0.332f
        // start the craniotomy at bregma
        private Vector3 _position = Vector3.zero;

        private float _size = 1f;
        private float _disabledSize = -1f;

        [SerializeField] TP_CraniotomySkull _craniotomySkull;

        private void Start()
        {
            // Start with craniotomy disabled
            UpdateSize(0);
            UpdateCraniotomy();
        }

        public void OnDisable()
        {
            _disabledSize = _size;
            UpdateSize(0);
        }

        public void OnEnable()
        {
            if (_disabledSize >= 0f)
                UpdateSize(_disabledSize);
            else
                UpdateSize(_size);
        }

        public void UpdateAP(float ap)
        {
            _position.x = ap;
            UpdateCraniotomy();
            UpdateText();
        }
        public void UpdateML(float ml)
        {
            _position.y = ml;
            UpdateCraniotomy();
            UpdateText();
        }
        public void UpdateSize(float newSize)
        {
            _size = newSize;
            UpdateCraniotomy();
            UpdateText();
        }

        private void UpdateText()
        {
            Vector3 pos = _TPManager.GetSetting_InVivoTransformActive() ? _TPManager.CoordinateTransformFromCCF(_position) : _position;

            _apText.text = "AP: " + Mathf.RoundToInt(pos.x * 1000f);
            _mlText.text = "ML: " + Mathf.RoundToInt(pos.y * 1000f);
            _radiusText.text = "r: " + Mathf.RoundToInt(_size * 1000f);
        }

        private void UpdateCraniotomy()
        {
            if (_craniotomySkull != null)
            {
                _craniotomySkull.SetCraniotomyPosition(_position);
                _craniotomySkull.SetCraniotomySize(_size);
            }
        }
    }

}