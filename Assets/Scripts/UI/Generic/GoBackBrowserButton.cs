using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuplex.WebView;


namespace Vortices
{
    public class GoBackBrowserButton : MonoBehaviour
    {
        public IWebView canvasWebView;

        public async void GoBack()
        {
            bool canGoBack = await canvasWebView.CanGoBack();
            if (canGoBack)
            {
                canvasWebView.GoBack();
            } 
        }
    }
}

