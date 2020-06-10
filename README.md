## APK
https://github.com/maxim-saplin/dopetest_xamarin/releases/download/1.0/maxim_saplin.dopetest_xamarin-Signed.apk

Note 10 Snapdragon 855. Android scehdulues Handler.post (used inside BeginInvokeOnMainThread) callbacks accroding to screen refresh rate (60Hz by default, ~16ms per frame), there's a trick in the loop's callback which does UI mutation in while loop and keeps track of 16ms elapsed time in loop to end it and schedule another callback.

![UI](https://github.com/maxim-saplin/dopetest_xamarin/blob/master/Screenshot_20200605-232224.jpg?raw=true)
 
