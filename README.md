# JSIAdvTransparentPods
Branch of original TransparentPods module that was part of Raster Prop Monitor mod for Kerbal Space Program.   
It is primarily for mod authors and mod part model authors to add Transparency capabilities to their parts.   
This mod adds Transparency capabilities allowing IVAs to be visible from external view both in the Editors and in Flight.   
Yes - Stock KSP 1.1 has a similar capability but this mod has more.      

The advantages of using this module over the stock overlay feature are:
* Any part/s with this module configured will ALWAYS show their IVA(unless the user toggles it to OFF or AUTO mode).   
* Through the LoadedInactive setting, IVAs will show for ALL loaded parts that have JSIAdvTransparentPod module, not just the active vessel (stock feature only turns on all the IVAs on the active vessel).   
* You can configure and swap shaders on the windows so they appear transparent or opaque and you can have window-like shaders and see the internals (stock feature does not allow this).  
* Works in Editor mode (although it does not support the DepthMask Shader meshes in Editor mode) you only see the entire IVA over the external when you turn TransparentPod mode ON in the editor.   
* Ability to have two different OVerlay modes - one for TransparentPod mode (say just transparent windows) and another for the stock overlay mode.   

Future plans include:-
Support for DepthMask shaders in the Editors.   

Please refer to the WIKI page for more information on how to use this module.   
https://github.com/JPLRepo/JSIAdvTransparentPods/wiki/JSIAdvTransparentPods
