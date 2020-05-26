﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;

namespace SmartLyrics
{
    class Animations
    {
        public static Animation BlinkingAnimation(int duration, int repeatCount)
        {
            Animation anim = new AlphaAnimation(0.2f, 1.0f);
            anim.Duration = duration; //You can manage the blinking time with this parameter
            anim.RepeatMode = RepeatMode.Reverse;
            anim.RepeatCount = repeatCount;

            return anim;
        }
    }
}