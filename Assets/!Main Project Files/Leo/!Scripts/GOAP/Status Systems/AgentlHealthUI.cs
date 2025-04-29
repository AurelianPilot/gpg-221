using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Main_Project_Files.Leo._Scripts.GOAP.Status_Systems
{
    /// <summary>
    /// Displays a health bar and optional stats above an agent.
    /// Attaches to a World Space Canvas that follows the agent.
    /// </summary>
    public class AgentHealthUI : MonoBehaviour
    {
        [Header("- References")]
        [SerializeField] private AgentHealthSystem healthSystem;

        [SerializeField] private Image healthBarFill;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI agentNameText;

        [Header("- Position Settings")]
        [SerializeField] private Vector3 offset = new(0, 1.5f, 0);

        [SerializeField] private bool lookAtCamera = true;

        [Header("- Appearance Settings")]
        [SerializeField] private Color healthyColor = new(0.2f, 0.8f, 0.2f);

        [SerializeField] private Color damagedColor = new(0.8f, 0.8f, 0.2f);
        [SerializeField] private Color criticalColor = new(0.8f, 0.2f, 0.2f);
        [SerializeField] private bool showHealthNumbers = true;
        [SerializeField] private bool showHealthPercent;
        
        private Camera _mainCamera;
        private Transform _agentTransform;
        private Canvas _worldSpaceCanvas;

        private void Awake() {
            if (healthSystem == null) {
                healthSystem = GetComponentInParent<AgentHealthSystem>();
            }

            if (healthSystem == null) {
                Debug.LogError(
                    "AgentHealthUI.cs: No AgentHealthSystem found in parent objects. Health UI will not function.");
                this.enabled = false;
                return;
            }

            _agentTransform = healthSystem.transform;
            _mainCamera = Camera.main;
            _worldSpaceCanvas = GetComponent<Canvas>();

            if (agentNameText != null) {
                agentNameText.text = _agentTransform.name;
            }
        }

        private void OnEnable() {
            if (healthSystem != null) {
                healthSystem.OnHealthChanged.AddListener(UpdateHealthBar);

                UpdateHealthBar(healthSystem.CurrentHealth, healthSystem.MaxHealth);
            }
        }

        private void OnDisable() {
            if (healthSystem != null) {
                healthSystem.OnHealthChanged.RemoveListener(UpdateHealthBar);
            }
        }

        private void LateUpdate() {
            UpdatePosition();
        }
        
        private void UpdatePosition() {
            if (_agentTransform == null) return;

            transform.position = _agentTransform.position + offset;

            if (lookAtCamera && _mainCamera != null) {
                transform.rotation = Quaternion.LookRotation(transform.position - _mainCamera.transform.position);
            }
        }
        
        private void UpdateHealthBar(float currentHealth, float maxHealth) {
            if (healthBarFill == null) return;

            float fillAmount = maxHealth > 0 ? currentHealth / maxHealth : 0;
            healthBarFill.fillAmount = fillAmount;

            healthBarFill.color = GetHealthBarColor(fillAmount);

            if (healthText != null && showHealthNumbers) {
                if (showHealthPercent) {
                    healthText.text = $"{fillAmount * 100:F0}%";
                }
                else {
                    healthText.text = $"{currentHealth:F0}/{maxHealth:F0}";
                }
            }
        }

        private Color GetHealthBarColor(float healthPercent) {
            if (healthPercent > 0.6f) {
                return healthyColor;
            }
            else if (healthPercent > 0.3f) {
                return damagedColor;
            }
            else {
                return criticalColor;
            }
        }

        public void ShowHealthUI(bool show) {
            gameObject.SetActive(show);
        }

        public void ShowHealthUITemporarily(float duration) {
            ShowHealthUI(true);
            Invoke(nameof(HideHealthUI), duration);
        }

        private void HideHealthUI() {
            ShowHealthUI(false);
        }
        
        public void SetShowHealthNumbers(bool show) {
            showHealthNumbers = show;

            if (healthText != null) {
                healthText.gameObject.SetActive(show);
            }

            if (healthSystem != null) {
                UpdateHealthBar(healthSystem.CurrentHealth, healthSystem.MaxHealth);
            }
        }
    }
}