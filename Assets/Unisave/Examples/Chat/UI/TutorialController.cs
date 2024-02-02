using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Unisave.Examples.Chat.UI
{
    public class TutorialController : MonoBehaviour
    {
        public List<GameObject> slides;
        
        public int currentSlide;

        public Button nextSlideButton;
        public Button prevSlideButton;

        void Start()
        {
            nextSlideButton.onClick.AddListener(NextSlide);
            prevSlideButton.onClick.AddListener(PrevSlide);
            
            RenderCurrentSlide();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                PrevSlide();
            
            if (Input.GetKeyDown(KeyCode.RightArrow))
                NextSlide();
        }

        public void NextSlide()
        {
            if (currentSlide >= slides.Count - 1)
                return;
            
            currentSlide += 1;
            RenderCurrentSlide();
        }

        public void PrevSlide()
        {
            if (currentSlide <= 0)
                return;
            
            currentSlide -= 1;
            RenderCurrentSlide();
        }

        void RenderCurrentSlide()
        {
            foreach (var s in slides)
                s.SetActive(false);
            
            slides[currentSlide].SetActive(true);

            nextSlideButton.interactable = (currentSlide != slides.Count - 1);
            prevSlideButton.interactable = (currentSlide != 0);
        }
    }
}
