// Made with Amplify Shader Editor v1.9.9.5
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Sprite Shaders Ultimate/GUI SSU"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0

        [Toggle( _ISTEXT_ON )] _IsText( "Is Text", Float ) = 0
        [Toggle( _ISTEXTMESHPRO_ON )] _IsTextMeshPro( "Is Text Mesh Pro", Float ) = 0
        _TMPThickness( "TMP: Thickness", Range( -0.99, 0 ) ) = -0.45
        _TMPSmoothness( "TMP: Smoothness", Float ) = 5
        [HideInInspector] _UIVertexColorAlwaysGammaSpace( "UIVertexColorAlwaysGammaSpace", Int ) = 0
        [Toggle( _VERTEXTINTFIRST_ON )] _VertexTintFirst( "Vertex Tint First", Float ) = 0
        [Toggle( _PIXELPERFECTSPACE_ON )] _PixelPerfectSpace( "Pixel Perfect Space", Float ) = 0
        [Toggle( _PIXELPERFECTUV_ON )] _PixelPerfectUV( "Pixel Perfect UV", Float ) = 0
        [Toggle( _SPRITESHEETFIX_ON )] _SpriteSheetFix( "Sprite Sheet Fix", Float ) = 0
        _SpriteSheetRect( "Sprite Sheet Rect", Vector ) = ( 0, 0, 1, 1 )
        [Toggle( _TILINGFIX_ON )] _TilingFix( "Tiling Fix", Float ) = 0
        [Toggle( _RESIZABLETEXTURE_ON )] _ResizableTexture( "Resizable Texture", Float ) = 0
        [Toggle( _BAKEDMATERIAL_ON )] _BakedMaterial( "Baked Material", Float ) = 0
        [KeywordEnum( UV,UV_Raw,Object,Object_Scaled,World,UI_Graphic,Screen )] _ShaderSpace( "Shader Space", Float ) = 0
        _PixelsPerUnit( "Pixels Per Unit", Float ) = 100
        _ScreenWidthUnits( "Screen Width Units", Float ) = 10
        _RectWidth( "Rect Width", Float ) = 100
        _RectHeight( "Rect Height", Float ) = 100
        _ShaderSpaceOffset( "Shader Space: Offset", Vector ) = ( 0, 0, 0, 0 )
        [Toggle( _SHADERSPACEPIXELATE_ON )] _ShaderSpacePixelate( "Shader Space: Pixelate", Float ) = 0
        _ShaderSpacePixelation( "Shader Space: Pixelation", Float ) = 16
        [KeywordEnum( None,Full,Mask,Dissolve,Spread )] _ShaderFading( "Shader Fading", Float ) = 0
        _FadingFade( "Fading: Fade", Range( 0, 1 ) ) = 1
        _FadingPosition( "Fading: Position", Vector ) = ( 0, 0, 0, 0 )
        _FadingWidth( "Fading: Width", Float ) = 0.3
        _FadingNoiseFactor( "Fading: Noise Factor", Float ) = 0.2
        _FadingNoiseScale( "Fading: Noise Scale", Vector ) = ( 0.2, 0.2, 0, 0 )
        _FadingMask( "Fading: Mask", 2D ) = "white" {}
        [Toggle( _TOGGLEUNSCALEDTIME_ON )] _ToggleUnscaledTime( "Toggle: Unscaled Time", Float ) = 0
        [Toggle( _TOGGLECUSTOMTIME_ON )] _ToggleCustomTime( "Toggle: Custom Time", Float ) = 0
        _TimeValue( "Time: Value", Float ) = 0
        [Toggle( _TOGGLETIMESPEED_ON )] _ToggleTimeSpeed( "Toggle: Time Speed", Float ) = 0
        _TimeSpeed( "Time: Speed", Float ) = 1
        [Toggle( _TOGGLETIMEFPS_ON )] _ToggleTimeFPS( "Toggle: Time FPS", Float ) = 0
        _TimeFPS( "Time: FPS", Float ) = 5
        [Toggle( _TOGGLETIMEFREQUENCY_ON )] _ToggleTimeFrequency( "Toggle: Time Frequency", Float ) = 0
        _TimeFrequency( "Time: Frequency", Float ) = 2
        _TimeRange( "Time: Range", Float ) = 0.5
        _UberNoiseTexture( "Uber Noise Texture", 2D ) = "white" {}
        [Toggle( _ENABLESTRONGTINT_ON )] _EnableStrongTint( "Enable Strong Tint", Float ) = 0
        _StrongTintFade( "Strong Tint: Fade", Range( 0, 1 ) ) = 1
        [HDR][NoAlpha] _StrongTintTint( "Strong Tint: Tint", Color ) = ( 1, 1, 1, 1 )
        [Toggle( _STRONGTINTCONTRASTTOGGLE_ON )] _StrongTintContrastToggle( "Strong Tint: Contrast Toggle", Float ) = 0
        _StrongTintContrast( "Strong Tint: Contrast", Float ) = 0
        [Toggle( _STRONGTINTMASKTOGGLE_ON )] _StrongTintMaskToggle( "Strong Tint: Mask Toggle", Float ) = 0
        _StrongTintMask( "Strong Tint: Mask", 2D ) = "white" {}
        [Toggle( _ENABLEADDCOLOR_ON )] _EnableAddColor( "Enable Add Color", Float ) = 0
        _AddColorFade( "Add Color: Fade", Range( 0, 1 ) ) = 1
        [HDR] _AddColorColor( "Add Color: Color", Color ) = ( 2.996078, 0, 0, 0 )
        [Toggle( _ADDCOLORCONTRASTTOGGLE_ON )] _AddColorContrastToggle( "Add Color: Contrast Toggle", Float ) = 0
        _AddColorContrast( "Add Color: Contrast", Float ) = 0.5
        [Toggle( _ADDCOLORMASKTOGGLE_ON )] _AddColorMaskToggle( "Add Color: Mask Toggle", Float ) = 0
        _AddColorMask( "Add Color: Mask", 2D ) = "white" {}
        [Toggle( _ENABLEALPHATINT_ON )] _EnableAlphaTint( "Enable Alpha Tint", Float ) = 0
        _AlphaTintFade( "Alpha Tint: Fade", Range( 0, 1 ) ) = 1
        [HDR] _AlphaTintColor( "Alpha Tint: Color", Color ) = ( 95.87451, 5.019608, 95.87451, 0 )
        _AlphaTintMinAlpha( "Alpha Tint: Min Alpha", Range( 0, 1 ) ) = 0.02
        [Toggle( _ENABLESHADOW_ON )] _EnableShadow( "Enable Shadow", Float ) = 0
        _ShadowFade( "Shadow: Fade", Range( 0, 1 ) ) = 1
        _ShadowOffset( "Shadow: Offset", Vector ) = ( 0.05, -0.05, 0, 0 )
        _ShadowColor( "Shadow: Color", Color ) = ( 0, 0, 0, 0 )
        [Toggle( _ENABLEBRIGHTNESS_ON )] _EnableBrightness( "Enable Brightness", Float ) = 0
        _Brightness( "Brightness", Float ) = 1
        [Toggle( _ENABLECONTRAST_ON )] _EnableContrast( "Enable Contrast", Float ) = 0
        _Contrast( "Contrast", Float ) = 1
        [Toggle( _ENABLESATURATION_ON )] _EnableSaturation( "Enable Saturation", Float ) = 0
        _Saturation( "Saturation", Float ) = 1
        [Toggle( _ENABLEHUE_ON )] _EnableHue( "Enable Hue", Float ) = 0
        _Hue( "Hue", Range( -1, 1 ) ) = 0
        [Toggle( _ENABLERECOLORRGB_ON )] _EnableRecolorRGB( "Enable Recolor RGB", Float ) = 0
        _RecolorRGBFade( "Recolor RGB: Fade", Range( 0, 1 ) ) = 1
        [HDR] _RecolorRGBRedTint( "Recolor RGB: Red Tint", Color ) = ( 1, 1, 1, 0.5019608 )
        [HDR] _RecolorRGBGreenTint( "Recolor RGB: Green Tint", Color ) = ( 1, 1, 1, 0.5019608 )
        [HDR] _RecolorRGBBlueTint( "Recolor RGB: Blue Tint", Color ) = ( 1, 1, 1, 0.5019608 )
        [Toggle( _RECOLORRGBTEXTURETOGGLE_ON )] _RecolorRGBTextureToggle( "Recolor RGB: Texture Toggle", Float ) = 0
        _RecolorRGBTexture( "Recolor RGB: Texture", 2D ) = "white" {}
        [Toggle( _ENABLERECOLORRGBYCP_ON )] _EnableRecolorRGBYCP( "Enable Recolor RGBYCP", Float ) = 0
        _RecolorRGBYCPFade( "Recolor RGBYCP: Fade", Range( 0, 1 ) ) = 1
        [HDR] _RecolorRGBYCPRedTint( "Recolor RGBYCP: Red Tint", Color ) = ( 1, 1, 1, 0.5019608 )
        [HDR] _RecolorRGBYCPYellowTint( "Recolor RGBYCP: Yellow Tint", Color ) = ( 1, 1, 1, 0.5019608 )
        [HDR] _RecolorRGBYCPGreenTint( "Recolor RGBYCP: Green Tint", Color ) = ( 1, 1, 1, 0.5019608 )
        [HDR] _RecolorRGBYCPCyanTint( "Recolor RGBYCP: Cyan Tint", Color ) = ( 1, 1, 1, 0.5019608 )
        [HDR] _RecolorRGBYCPBlueTint( "Recolor RGBYCP: Blue Tint", Color ) = ( 1, 1, 1, 0.5019608 )
        [HDR] _RecolorRGBYCPPurpleTint( "Recolor RGBYCP: Purple Tint", Color ) = ( 1, 1, 1, 0.5019608 )
        [Toggle( _RECOLORRGBYCPTEXTURETOGGLE_ON )] _RecolorRGBYCPTextureToggle( "Recolor RGBYCP: Texture Toggle", Float ) = 0
        _RecolorRGBYCPTexture( "Recolor RGBYCP: Texture", 2D ) = "white" {}
        [Toggle( _ENABLEINNEROUTLINE_ON )] _EnableInnerOutline( "Enable Inner Outline", Float ) = 0
        _InnerOutlineFade( "Inner Outline: Fade", Range( 0, 1 ) ) = 1
        [HDR] _InnerOutlineColor( "Inner Outline: Color", Color ) = ( 11.98431, 1.254902, 1.254902, 1 )
        _InnerOutlineWidth( "Inner Outline: Width", Float ) = 0.02
        [Toggle( _INNEROUTLINEDISTORTIONTOGGLE_ON )] _InnerOutlineDistortionToggle( "Inner Outline: Distortion Toggle", Float ) = 0
        _InnerOutlineDistortionIntensity( "Inner Outline: Distortion Intensity", Vector ) = ( 0.01, 0.01, 0, 0 )
        _InnerOutlineNoiseScale( "Inner Outline: Noise Scale", Vector ) = ( 4, 4, 0, 0 )
        _InnerOutlineNoiseSpeed( "Inner Outline: Noise Speed", Vector ) = ( 0, 0.1, 0, 0 )
        [Toggle( _INNEROUTLINETEXTURETOGGLE_ON )] _InnerOutlineTextureToggle( "Inner Outline: Texture Toggle", Float ) = 0
        _InnerOutlineTintTexture( "Inner Outline: Tint Texture", 2D ) = "white" {}
        _InnerOutlineTextureSpeed( "Inner Outline: Texture Speed", Vector ) = ( 0.5, 0, 0, 0 )
        [Toggle( _INNEROUTLINEOUTLINEONLYTOGGLE_ON )] _InnerOutlineOutlineOnlyToggle( "Inner Outline: Outline Only Toggle", Float ) = 0
        [Toggle( _ENABLEOUTEROUTLINE_ON )] _EnableOuterOutline( "Enable Outer Outline", Float ) = 0
        _OuterOutlineFade( "Outer Outline: Fade", Range( 0, 1 ) ) = 1
        [HDR] _OuterOutlineColor( "Outer Outline: Color", Color ) = ( 0, 0, 0, 1 )
        _OuterOutlineWidth( "Outer Outline: Width", Float ) = 0.04
        [Toggle( _OUTEROUTLINEDISTORTIONTOGGLE_ON )] _OuterOutlineDistortionToggle( "Outer Outline: Distortion Toggle", Float ) = 0
        _OuterOutlineDistortionIntensity( "Outer Outline: Distortion Intensity", Vector ) = ( 0.01, 0.01, 0, 0 )
        _OuterOutlineNoiseScale( "Outer Outline: Noise Scale", Vector ) = ( 4, 4, 0, 0 )
        _OuterOutlineNoiseSpeed( "Outer Outline: Noise Speed", Vector ) = ( 0, 0.1, 0, 0 )
        [Toggle( _OUTEROUTLINETEXTURETOGGLE_ON )] _OuterOutlineTextureToggle( "Outer Outline: Texture Toggle", Float ) = 0
        _OuterOutlineTintTexture( "Outer Outline: Tint Texture", 2D ) = "white" {}
        _OuterOutlineTextureSpeed( "Outer Outline: Texture Speed", Vector ) = ( 0.5, 0, 0, 0 )
        [Toggle( _OUTEROUTLINEOUTLINEONLYTOGGLE_ON )] _OuterOutlineOutlineOnlyToggle( "Outer Outline: Outline Only Toggle", Float ) = 0
        [Toggle( _ENABLEPIXELOUTLINE_ON )] _EnablePixelOutline( "Enable Pixel Outline", Float ) = 0
        _PixelOutlineFade( "Pixel Outline: Fade", Range( 0, 1 ) ) = 1
        [HDR] _PixelOutlineColor( "Pixel Outline: Color", Color ) = ( 0, 0, 0, 1 )
        _PixelOutlineWidth( "Pixel Outline: Width", Float ) = 1
        _PixelOutlineAlphaLimit( "Pixel Outline: Alpha Limit", Range( 0, 1 ) ) = 0.5
        [Toggle( _PIXELOUTLINETEXTURETOGGLE_ON )] _PixelOutlineTextureToggle( "Pixel Outline: Texture Toggle", Float ) = 0
        _PixelOutlineTintTexture( "Pixel Outline: Tint Texture", 2D ) = "white" {}
        _PixelOutlineTextureSpeed( "Pixel Outline: Texture Speed", Vector ) = ( 0.5, 0, 0, 0 )
        [Toggle( _PIXELOUTLINEOUTLINEONLYTOGGLE_ON )] _PixelOutlineOutlineOnlyToggle( "Pixel Outline: Outline Only Toggle", Float ) = 0
        [Toggle( _ENABLEADDHUE_ON )] _EnableAddHue( "Enable Add Hue", Float ) = 0
        _AddHueFade( "Add Hue: Fade", Range( 0, 1 ) ) = 1
        _AddHueSpeed( "Add Hue: Speed", Float ) = 1
        _AddHueBrightness( "Add Hue: Brightness", Float ) = 2
        _AddHueSaturation( "Add Hue: Saturation", Range( 0, 1 ) ) = 1
        _AddHueContrast( "Add Hue: Contrast", Float ) = 0.5
        [Toggle( _ADDHUEMASKTOGGLE_ON )] _AddHueMaskToggle( "Add Hue: Mask Toggle", Float ) = 0
        _AddHueMask( "Add Hue: Mask", 2D ) = "white" {}
        [Toggle( _ENABLEPINGPONGGLOW_ON )] _EnablePingPongGlow( "Enable Ping-Pong Glow", Float ) = 0
        _PingPongGlowFade( "Ping-Pong Glow: Fade", Range( 0, 1 ) ) = 1
        [HDR] _PingPongGlowFrom( "Ping-Pong Glow: From", Color ) = ( 5.992157, 0.1882353, 0.1882353, 0 )
        [HDR] _PingPongGlowTo( "Ping-Pong Glow: To", Color ) = ( 0.1882353, 0.1882353, 5.992157, 0 )
        _PingPongGlowFrequency( "Ping-Pong Glow: Frequency", Float ) = 3
        _PingPongGlowContrast( "Ping-Pong Glow: Contrast", Float ) = 1
        [Toggle( _ENABLESHIFTHUE_ON )] _EnableShiftHue( "Enable Shift Hue", Float ) = 0
        _ShiftHueSpeed( "Shift Hue: Speed", Float ) = 0.5
        [Toggle( _ENABLEINKSPREAD_ON )] _EnableInkSpread( "Enable Ink Spread", Float ) = 0
        _InkSpreadFade( "Ink Spread: Fade", Range( 0, 1 ) ) = 1
        [HDR] _InkSpreadColor( "Ink Spread: Color", Color ) = ( 8.47419, 5.013525, 0.08873497, 0 )
        _InkSpreadContrast( "Ink Spread: Contrast", Float ) = 2
        _InkSpreadDistance( "Ink Spread: Distance", Float ) = 3
        _InkSpreadPosition( "Ink Spread: Position", Vector ) = ( 0.5, -1, 0, 0 )
        _InkSpreadWidth( "Ink Spread: Width", Float ) = 0.2
        _InkSpreadNoiseScale( "Ink Spread: Noise Scale", Vector ) = ( 0.4, 0.4, 0, 0 )
        _InkSpreadNoiseFactor( "Ink Spread: Noise Factor", Float ) = 0.5
        [Toggle( _ENABLEBLACKTINT_ON )] _EnableBlackTint( "Enable Black Tint", Float ) = 0
        _BlackTintFade( "Black Tint: Fade", Range( 0, 1 ) ) = 1
        [HDR] _BlackTintColor( "Black Tint: Color", Color ) = ( 0, 0, 1, 0 )
        _BlackTintPower( "Black Tint: Power", Float ) = 4
        [Toggle( _ENABLESINEGLOW_ON )] _EnableSineGlow( "Enable Sine Glow", Float ) = 0
        _SineGlowFade( "Sine Glow: Fade", Range( 0, 1 ) ) = 1
        [HDR] _SineGlowColor( "Sine Glow: Color", Color ) = ( 0, 2.007843, 2.996078, 0 )
        _SineGlowContrast( "Sine Glow: Contrast", Float ) = 1
        _SineGlowFrequency( "Sine Glow: Frequency", Float ) = 4
        _SineGlowMin( "Sine Glow: Min", Float ) = 0
        _SineGlowMax( "Sine Glow: Max", Float ) = 1
        [Toggle( _SINEGLOWMASKTOGGLE_ON )] _SineGlowMaskToggle( "Sine Glow: Mask Toggle", Float ) = 0
        _SineGlowMask( "Sine Glow: Mask", 2D ) = "white" {}
        [Toggle( _ENABLESPLITTONING_ON )] _EnableSplitToning( "Enable Split Toning", Float ) = 0
        _SplitToningFade( "Split Toning: Fade", Range( 0, 1 ) ) = 1
        [HDR] _SplitToningHighlightsColor( "Split Toning: Highlights Color", Color ) = ( 1, 0.1, 0.1, 0 )
        [HDR] _SplitToningShadowsColor( "Split Toning: Shadows Color", Color ) = ( 0.1, 0.4000002, 1, 0 )
        _SplitToningContrast( "Split Toning: Contrast", Float ) = 1
        _SplitToningBalance( "Split Toning: Balance", Float ) = 1
        _SplitToningShift( "Split Toning: Shift", Range( -1, 1 ) ) = 0
        [Toggle( _ENABLECOLORREPLACE_ON )] _EnableColorReplace( "Enable Color Replace", Float ) = 0
        _ColorReplaceFade( "Color Replace: Fade", Range( 0, 1 ) ) = 1
        _ColorReplaceFromColor( "Color Replace: From Color", Color ) = ( 0, 0, 0, 0 )
        [HDR] _ColorReplaceToColor( "Color Replace: To Color", Color ) = ( 0, 0, 0.2, 0 )
        _ColorReplaceRange( "Color Replace: Range", Float ) = 0.05
        _ColorReplaceSmoothness( "Color Replace: Smoothness", Float ) = 0.1
        _ColorReplaceContrast( "Color Replace: Contrast", Float ) = 1
        [Toggle( _ENABLENEGATIVE_ON )] _EnableNegative( "Enable Negative", Float ) = 0
        _NegativeFade( "Negative: Fade", Range( 0, 1 ) ) = 1
        [Toggle( _ENABLEHOLOGRAM_ON )] _EnableHologram( "Enable Hologram", Float ) = 0
        _HologramFade( "Hologram: Fade", Range( 0, 1 ) ) = 1
        [HDR] _HologramTint( "Hologram: Tint", Color ) = ( 0.3137255, 1.662745, 2.996078, 1 )
        _HologramContrast( "Hologram: Contrast", Float ) = 1
        _HologramLineFrequency( "Hologram: Line Frequency", Float ) = 500
        _HologramLineGap( "Hologram: Line Gap", Range( 0, 5 ) ) = 3
        _HologramLineSpeed( "Hologram: Line Speed", Float ) = 0.01
        _HologramMinAlpha( "Hologram: Min Alpha", Range( 0, 1 ) ) = 0.2
        _HologramDistortionOffset( "Hologram: Distortion Offset", Float ) = 0.5
        _HologramDistortionSpeed( "Hologram: Distortion Speed", Float ) = 2
        _HologramDistortionDensity( "Hologram: Distortion Density", Float ) = 0.5
        _HologramDistortionScale( "Hologram: Distortion Scale", Float ) = 10
        [Toggle( _ENABLEGLITCH_ON )] _EnableGlitch( "Enable Glitch", Float ) = 0
        _GlitchFade( "Glitch: Fade", Range( 0, 1 ) ) = 1
        _GlitchMaskMin( "Glitch: Mask Min", Range( 0, 1 ) ) = 0.4
        _GlitchMaskScale( "Glitch: Mask Scale", Vector ) = ( 0, 0.2, 0, 0 )
        _GlitchMaskSpeed( "Glitch: Mask Speed", Vector ) = ( 0, 4, 0, 0 )
        _GlitchHueSpeed( "Glitch: Hue Speed", Float ) = 1
        _GlitchBrightness( "Glitch: Brightness", Float ) = 4
        _GlitchNoiseScale( "Glitch: Noise Scale", Vector ) = ( 0, 3, 0, 0 )
        _GlitchNoiseSpeed( "Glitch: Noise Speed", Vector ) = ( 0, 1, 0, 0 )
        _GlitchDistortion( "Glitch: Distortion", Vector ) = ( 0.1, 0, 0, 0 )
        _GlitchDistortionScale( "Glitch: Distortion Scale", Vector ) = ( 0, 3, 0, 0 )
        _GlitchDistortionSpeed( "Glitch: Distortion Speed", Vector ) = ( 0, 1, 0, 0 )
        [Toggle( _ENABLEFROZEN_ON )] _EnableFrozen( "Enable Frozen", Float ) = 0
        _FrozenFade( "Frozen: Fade", Range( 0, 1 ) ) = 1
        [HDR] _FrozenTint( "Frozen: Tint", Color ) = ( 1.819608, 4.611765, 5.992157, 0 )
        _FrozenContrast( "Frozen: Contrast", Float ) = 2
        [HDR] _FrozenSnowColor( "Frozen: Snow Color", Color ) = ( 1.123529, 1.373203, 1.498039, 0 )
        _FrozenSnowContrast( "Frozen: Snow Contrast", Float ) = 1
        _FrozenSnowDensity( "Frozen: Snow Density", Range( 0, 1 ) ) = 0.25
        _FrozenSnowScale( "Frozen: Snow Scale", Vector ) = ( 0.1, 0.1, 0, 0 )
        [HDR] _FrozenHighlightColor( "Frozen: Highlight Color", Color ) = ( 1.797647, 4.604501, 5.992157, 1 )
        _FrozenHighlightContrast( "Frozen: Highlight Contrast", Float ) = 2
        _FrozenHighlightDensity( "Frozen: Highlight Density", Range( 0, 1 ) ) = 1
        _FrozenHighlightSpeed( "Frozen: Highlight Speed", Vector ) = ( 0.1, 0.1, 0, 0 )
        _FrozenHighlightScale( "Frozen: Highlight Scale", Vector ) = ( 0.2, 0.2, 0, 0 )
        _FrozenHighlightDistortion( "Frozen: Highlight Distortion", Vector ) = ( 0.5, 0.5, 0, 0 )
        _FrozenHighlightDistortionSpeed( "Frozen: Highlight Distortion Speed", Vector ) = ( -0.05, -0.05, 0, 0 )
        _FrozenHighlightDistortionScale( "Frozen: Highlight Distortion Scale", Vector ) = ( 0.2, 0.2, 0, 0 )
        [Toggle( _ENABLERAINBOW_ON )] _EnableRainbow( "Enable Rainbow", Float ) = 0
        _RainbowFade( "Rainbow: Fade", Range( 0, 1 ) ) = 1
        _RainbowBrightness( "Rainbow: Brightness", Float ) = 2
        _RainbowSaturation( "Rainbow: Saturation", Range( 0, 1 ) ) = 1
        _RainbowContrast( "Rainbow: Contrast", Float ) = 1
        _RainbowSpeed( "Rainbow: Speed", Float ) = 1
        _RainbowDensity( "Rainbow: Density", Float ) = 0.5
        _RainbowCenter( "Rainbow: Center", Vector ) = ( 0, 0, 0, 0 )
        _RainbowNoiseScale( "Rainbow: Noise Scale", Vector ) = ( 0.2, 0.2, 0, 0 )
        _RainbowNoiseFactor( "Rainbow: Noise Factor", Float ) = 0.2
        [Toggle( _ENABLECAMOUFLAGE_ON )] _EnableCamouflage( "Enable Camouflage", Float ) = 0
        _CamouflageFade( "Camouflage: Fade", Range( 0, 1 ) ) = 1
        _CamouflageBaseColor( "Camouflage: Base Color", Color ) = ( 0.7450981, 0.7254902, 0.5686275, 0 )
        _CamouflageContrast( "Camouflage: Contrast", Float ) = 1
        _CamouflageColorA( "Camouflage: Color A", Color ) = ( 0.627451, 0.5882353, 0.4313726, 0 )
        _CamouflageDensityA( "Camouflage: Density A", Range( 0, 1 ) ) = 0.4
        _CamouflageSmoothnessA( "Camouflage: Smoothness A", Range( 0, 1 ) ) = 0.2
        _CamouflageNoiseScaleA( "Camouflage: Noise Scale A", Vector ) = ( 0.25, 0.25, 0, 0 )
        _CamouflageColorB( "Camouflage: Color B", Color ) = ( 0.4705882, 0.4313726, 0.3137255, 0 )
        _CamouflageDensityB( "Camouflage: Density B", Range( 0, 1 ) ) = 0.4
        _CamouflageSmoothnessB( "Camouflage: Smoothness B", Range( 0, 1 ) ) = 0.2
        _CamouflageNoiseScaleB( "Camouflage: Noise Scale B", Vector ) = ( 0.25, 0.25, 0, 0 )
        [Toggle( _CAMOUFLAGEANIMATIONTOGGLE_ON )] _CamouflageAnimationToggle( "Camouflage: Animation Toggle", Float ) = 0
        _CamouflageDistortionSpeed( "Camouflage: Distortion Speed", Vector ) = ( 0.1, 0.1, 0, 0 )
        _CamouflageDistortionIntensity( "Camouflage: Distortion Intensity", Vector ) = ( 0.1, 0.1, 0, 0 )
        _CamouflageDistortionScale( "Camouflage: Distortion Scale", Vector ) = ( 0.5, 0.5, 0, 0 )
        [Toggle( _ENABLEMETAL_ON )] _EnableMetal( "Enable Metal", Float ) = 0
        _MetalFade( "Metal: Fade", Range( 0, 1 ) ) = 1
        [HDR] _MetalColor( "Metal: Color", Color ) = ( 5.992157, 3.639216, 0.3137255, 1 )
        _MetalContrast( "Metal: Contrast", Float ) = 2
        [HDR] _MetalHighlightColor( "Metal: Highlight Color", Color ) = ( 5.992157, 3.796078, 0.6588235, 1 )
        _MetalHighlightDensity( "Metal: Highlight Density", Range( 0, 1 ) ) = 1
        _MetalHighlightContrast( "Metal: Highlight Contrast", Float ) = 2
        _MetalNoiseScale( "Metal: Noise Scale", Vector ) = ( 0.25, 0.25, 0, 0 )
        _MetalNoiseSpeed( "Metal: Noise Speed", Vector ) = ( 0.05, 0.05, 0, 0 )
        _MetalNoiseDistortionScale( "Metal: Noise Distortion Scale", Vector ) = ( 0.2, 0.2, 0, 0 )
        _MetalNoiseDistortionSpeed( "Metal: Noise Distortion Speed", Vector ) = ( -0.05, -0.05, 0, 0 )
        _MetalNoiseDistortion( "Metal: Noise Distortion", Vector ) = ( 0.5, 0.5, 0, 0 )
        [Toggle( _METALMASKTOGGLE_ON )] _MetalMaskToggle( "Metal: Mask Toggle", Float ) = 0
        [NoScaleOffset] _MetalMask( "Metal: Mask", 2D ) = "white" {}
        [Toggle( _ENABLESHINE_ON )] _EnableShine( "Enable Shine", Float ) = 0
        _ShineFade( "Shine: Fade", Range( 0, 1 ) ) = 1
        [HDR] _ShineColor( "Shine: Color", Color ) = ( 11.98431, 11.98431, 11.98431, 0 )
        _ShineSaturation( "Shine: Saturation", Range( 0, 1 ) ) = 0.5
        _ShineContrast( "Shine: Contrast", Float ) = 2
        _ShineWidth( "Shine: Width", Float ) = 0.1
        _ShineSpeed( "Shine: Speed", Float ) = 5
        _ShineRotation( "Shine: Rotation", Range( 0, 360 ) ) = 30
        _ShineSmooth( "Shine: Smoothness", Float ) = 1
        _ShineFrequency( "Shine: Frequency", Float ) = 0.3
        [Toggle( _SHINEMASKTOGGLE_ON )] _ShineMaskToggle( "Shine: Mask Toggle", Float ) = 0
        [NoScaleOffset] _ShineMask( "Shine: Mask", 2D ) = "white" {}
        [Toggle( _ENABLEBURN_ON )] _EnableBurn( "Enable Burn", Float ) = 0
        _BurnFade( "Burn: Fade", Range( 0, 1 ) ) = 1
        _BurnPosition( "Burn: Position", Vector ) = ( 0, 5, 0, 0 )
        _BurnRadius( "Burn: Radius", Float ) = 5
        [HDR] _BurnEdgeColor( "Burn: Edge Color", Color ) = ( 11.98431, 1.129412, 0.1254902, 0 )
        _BurnWidth( "Burn: Width", Float ) = 0.1
        _BurnEdgeNoiseScale( "Burn: Edge Noise Scale", Vector ) = ( 0.3, 0.3, 0, 0 )
        _BurnEdgeNoiseFactor( "Burn: Edge Noise Factor", Float ) = 0.5
        [HDR] _BurnInsideColor( "Burn: Inside Color", Color ) = ( 0.75, 0.5625, 0.525, 0 )
        _BurnInsideContrast( "Burn: Inside Contrast", Float ) = 2
        [HDR] _BurnInsideNoiseColor( "Burn: Inside Noise Color", Color ) = ( 3084.047, 257.0039, 0, 0 )
        _BurnInsideNoiseFactor( "Burn: Inside Noise Factor", Float ) = 0.2
        _BurnInsideNoiseScale( "Burn: Inside Noise Scale", Vector ) = ( 0.5, 0.5, 0, 0 )
        _BurnSwirlFactor( "Burn: Swirl Factor", Float ) = 1
        _BurnSwirlNoiseScale( "Burn: Swirl Noise Scale", Vector ) = ( 0.1, 0.1, 0, 0 )
        [Toggle( _ENABLEPOISON_ON )] _EnablePoison( "Enable Poison", Float ) = 0
        _PoisonFade( "Poison: Fade", Range( 0, 1 ) ) = 1
        [HDR] _PoisonColor( "Poison: Color", Color ) = ( 0.3137255, 2.996078, 0.3137255, 0 )
        _PoisonDensity( "Poison: Density", Float ) = 3
        _PoisonRecolorFactor( "Poison: Recolor Factor", Range( 0, 1 ) ) = 0.5
        _PoisonShiftSpeed( "Poison: Shift Speed", Float ) = 0.2
        _PoisonNoiseBrightness( "Poison: Noise Brightness", Float ) = 2
        _PoisonNoiseScale( "Poison: Noise Scale", Vector ) = ( 0.2, 0.2, 0, 0 )
        _PoisonNoiseSpeed( "Poison: Noise Speed", Vector ) = ( 0, -0.2, 0, 0 )
        [Toggle( _ENABLEENCHANTED_ON )] _EnableEnchanted( "Enable Enchanted", Float ) = 0
        _EnchantedFade( "Enchanted: Fade", Range( 0, 1 ) ) = 1
        _EnchantedSpeed( "Enchanted: Speed", Vector ) = ( 0, 1, 0, 0 )
        _EnchantedScale( "Enchanted: Scale", Vector ) = ( 0.1, 0.1, 0, 0 )
        _EnchantedBrightness( "Enchanted: Brightness", Float ) = 1
        _EnchantedContrast( "Enchanted: Contrast", Float ) = 0.5
        _EnchantedReduce( "Enchanted: Reduce", Range( 0, 2 ) ) = 0
        [Toggle( _ENCHANTEDRAINBOWTOGGLE_ON )] _EnchantedRainbowToggle( "Enchanted: Rainbow Toggle", Float ) = 0
        _EnchantedRainbowSpeed( "Enchanted: Rainbow Speed", Float ) = 0.5
        _EnchantedRainbowDensity( "Enchanted: Rainbow Density", Float ) = 0.5
        _EnchantedRainbowSaturation( "Enchanted: Rainbow Saturation", Float ) = 0.8
        [HDR] _EnchantedLowColor( "Enchanted: Low Color", Color ) = ( 2.996078, 0, 0, 0 )
        [HDR] _EnchantedHighColor( "Enchanted: High Color", Color ) = ( 0, 0.7098798, 4.237095, 0 )
        [Toggle( _ENCHANTEDLERPTOGGLE_ON )] _EnchantedLerpToggle( "Enchanted: Lerp Toggle", Float ) = 0
        [Toggle( _ENABLESHIFTING_ON )] _EnableShifting( "Enable Shifting", Float ) = 0
        _ShiftingFade( "Shifting: Fade", Range( 0, 1 ) ) = 1
        _ShiftingSpeed( "Shifting: Speed", Float ) = 0.5
        _ShiftingDensity( "Shifting: Density", Float ) = 1.5
        _ShiftingBrightness( "Shifting: Brightness", Float ) = 1
        _ShiftingContrast( "Shifting: Contrast", Float ) = 0.5
        [Toggle( _SHIFTINGRAINBOWTOGGLE_ON )] _ShiftingRainbowToggle( "Shifting: Rainbow Toggle", Float ) = 0
        _ShiftingSaturation( "Shifting: Saturation", Float ) = 0.8
        [HDR] _ShiftingColorA( "Shifting: Color A", Color ) = ( 1.498039, 0, 0, 0 )
        [HDR] _ShiftingColorB( "Shifting: Color B", Color ) = ( 1.498039, 0.7490196, 0, 0 )
        [Toggle( _ENABLETEXTURELAYER1_ON )] _EnableTextureLayer1( "Enable Texture Layer 1", Float ) = 0
        _TextureLayer1Fade( "Texture Layer 1: Fade", Range( 0, 1 ) ) = 1
        _TextureLayer1Texture( "Texture Layer 1: Texture", 2D ) = "white" {}
        [HDR] _TextureLayer1Color( "Texture Layer 1: Color", Color ) = ( 0.4494117, 1.298301, 2.996079, 1 )
        _TextureLayer1Scale( "Texture Layer 1: Scale", Vector ) = ( 1, 1, 0, 0 )
        _TextureLayer1Offset( "Texture Layer 1: Offset", Vector ) = ( 0, 0, 0, 0 )
        [Toggle( _TEXTURELAYER1CONTRASTTOGGLE_ON )] _TextureLayer1ContrastToggle( "Texture Layer 1: Contrast Toggle", Float ) = 0
        _TextureLayer1Contrast( "Texture Layer 1: Contrast", Float ) = 1
        [Toggle( _TEXTURELAYER1SCROLLTOGGLE_ON )] _TextureLayer1ScrollToggle( "Texture Layer 1: Scroll Toggle", Float ) = 0
        _TextureLayer1ScrollSpeed( "Texture Layer 1: Scroll Speed", Vector ) = ( 0, 1, 0, 0 )
        [Toggle( _TEXTURELAYER1SHEETTOGGLE_ON )] _TextureLayer1SheetToggle( "Texture Layer 1: Sheet Toggle", Float ) = 1
        _TextureLayer1Columns( "Texture Layer 1: Columns", Int ) = 3
        _TextureLayer1Rows( "Texture Layer 1: Rows", Int ) = 3
        _TextureLayer1Speed( "Texture Layer 1: Speed", Float ) = 20
        _TextureLayer1StartFrame( "Texture Layer 1: Start Frame", Int ) = 0
        _TextureLayer1EdgeClip( "Texture Layer 1: Edge Clip", Range( 0, 1 ) ) = 0.005
        [Toggle( _ENABLETEXTURELAYER2_ON )] _EnableTextureLayer2( "Enable Texture Layer 2", Float ) = 0
        _TextureLayer2Fade( "Texture Layer 2: Fade", Range( 0, 1 ) ) = 1
        _TextureLayer2Texture( "Texture Layer 2: Texture", 2D ) = "white" {}
        [HDR] _TextureLayer2Color( "Texture Layer 2: Color", Color ) = ( 0.4494117, 1.298301, 2.996079, 1 )
        _TextureLayer2Scale( "Texture Layer 2: Scale", Vector ) = ( 1, 1, 0, 0 )
        _TextureLayer2Offset( "Texture Layer 2: Offset", Vector ) = ( 0, 0, 0, 0 )
        [Toggle( _TEXTURELAYER2CONTRASTTOGGLE_ON )] _TextureLayer2ContrastToggle( "Texture Layer 2: Contrast Toggle", Float ) = 0
        _TextureLayer2Contrast( "Texture Layer 2: Contrast", Float ) = 1
        [Toggle( _TEXTURELAYER2SCROLLTOGGLE_ON )] _TextureLayer2ScrollToggle( "Texture Layer 2: Scroll Toggle", Float ) = 0
        _TextureLayer2ScrollSpeed( "Texture Layer 2: Scroll Speed", Vector ) = ( 0, 1, 0, 0 )
        [Toggle( _TEXTURELAYER2SHEETTOGGLE_ON )] _TextureLayer2SheetToggle( "Texture Layer 2: Sheet Toggle", Float ) = 1
        _TextureLayer2Columns( "Texture Layer 2: Columns", Int ) = 3
        _TextureLayer2Rows( "Texture Layer 2: Rows", Int ) = 3
        _TextureLayer2Speed( "Texture Layer 2: Speed", Float ) = 20
        _TextureLayer2StartFrame( "Texture Layer 2: Start Frame", Int ) = 0
        _TextureLayer2EdgeClip( "Texture Layer 2: Edge Clip", Range( 0, 1 ) ) = 0.005
        [Toggle( _ENABLEFULLALPHADISSOLVE_ON )] _EnableFullAlphaDissolve( "Enable Full Alpha Dissolve", Float ) = 0
        _FullAlphaDissolveFade( "Full Alpha Dissolve: Fade", Range( 0, 1 ) ) = 0.5
        _FullAlphaDissolveWidth( "Full Alpha Dissolve: Width", Float ) = 0.5
        _FullAlphaDissolveNoiseScale( "Full Alpha Dissolve: Noise Scale", Vector ) = ( 0.1, 0.1, 0, 0 )
        [Toggle( _ENABLEFULLGLOWDISSOLVE_ON )] _EnableFullGlowDissolve( "Enable Full Glow Dissolve", Float ) = 0
        _FullGlowDissolveFade( "Full Glow Dissolve: Fade", Range( 0, 1 ) ) = 0.5
        _FullGlowDissolveWidth( "Full Glow Dissolve: Width", Float ) = 0.5
        [HDR] _FullGlowDissolveEdgeColor( "Full Glow Dissolve: Edge Color", Color ) = ( 11.98431, 0.627451, 0.627451, 0 )
        _FullGlowDissolveNoiseScale( "Full Glow Dissolve: Noise Scale", Vector ) = ( 0.1, 0.1, 0, 0 )
        [Toggle( _ENABLESOURCEALPHADISSOLVE_ON )] _EnableSourceAlphaDissolve( "Enable Source Alpha Dissolve", Float ) = 0
        _SourceAlphaDissolveFade( "Source Alpha Dissolve: Fade", Float ) = 1
        _SourceAlphaDissolvePosition( "Source Alpha Dissolve: Position", Vector ) = ( 0, 0, 0, 0 )
        _SourceAlphaDissolveWidth( "Source Alpha Dissolve: Width", Float ) = 0.2
        _SourceAlphaDissolveNoiseScale( "Source Alpha Dissolve: Noise Scale", Vector ) = ( 0.3, 0.3, 0, 0 )
        _SourceAlphaDissolveNoiseFactor( "Source Alpha Dissolve: Noise Factor", Float ) = 0.2
        [Toggle] _SourceAlphaDissolveInvert( "Source Alpha Dissolve: Invert", Float ) = 0
        [Toggle( _ENABLESOURCEGLOWDISSOLVE_ON )] _EnableSourceGlowDissolve( "Enable Source Glow Dissolve", Float ) = 0
        _SourceGlowDissolveFade( "Source Glow Dissolve: Fade", Float ) = 1
        _SourceGlowDissolvePosition( "Source Glow Dissolve: Position", Vector ) = ( 0, 0, 0, 0 )
        _SourceGlowDissolveWidth( "Source Glow Dissolve: Width", Float ) = 0.1
        [HDR] _SourceGlowDissolveEdgeColor( "Source Glow Dissolve: Edge Color", Color ) = ( 11.98431, 0.627451, 0.627451, 0 )
        _SourceGlowDissolveNoiseScale( "Source Glow Dissolve: Noise Scale", Vector ) = ( 0.3, 0.3, 0, 0 )
        _SourceGlowDissolveNoiseFactor( "Source Glow Dissolve: Noise Factor", Float ) = 0.2
        [Toggle] _SourceGlowDissolveInvert( "Source Glow Dissolve: Invert", Float ) = 0
        [Toggle( _ENABLEHALFTONE_ON )] _EnableHalftone( "Enable Halftone", Float ) = 0
        _HalftoneFade( "Halftone: Fade", Float ) = 1
        _HalftonePosition( "Halftone: Position", Vector ) = ( 0, 0, 0, 0 )
        _HalftoneTiling( "Halftone: Tiling", Float ) = 4
        _HalftoneFadeWidth( "Halftone: Width", Float ) = 1.5
        [Toggle] _HalftoneInvert( "Halftone: Invert", Float ) = 0
        [Toggle( _ENABLEDIRECTIONALALPHAFADE_ON )] _EnableDirectionalAlphaFade( "Enable Directional Alpha Fade", Float ) = 0
        _DirectionalAlphaFadeFade( "Directional Alpha Fade: Fade", Float ) = 0
        _DirectionalAlphaFadeRotation( "Directional Alpha Fade: Rotation", Range( 0, 360 ) ) = 0
        _DirectionalAlphaFadeWidth( "Directional Alpha Fade: Width", Float ) = 0.2
        _DirectionalAlphaFadeNoiseScale( "Directional Alpha Fade: Noise Scale", Vector ) = ( 0.3, 0.3, 0, 0 )
        _DirectionalAlphaFadeNoiseFactor( "Directional Alpha Fade: Noise Factor", Float ) = 0.2
        [Toggle] _DirectionalAlphaFadeInvert( "Directional Alpha Fade: Invert", Float ) = 0
        [Toggle( _ENABLEDIRECTIONALGLOWFADE_ON )] _EnableDirectionalGlowFade( "Enable Directional Glow Fade", Float ) = 0
        _DirectionalGlowFadeFade( "Directional Glow Fade: Fade", Float ) = 0
        _DirectionalGlowFadeRotation( "Directional Glow Fade: Rotation", Range( 0, 360 ) ) = 0
        [HDR] _DirectionalGlowFadeEdgeColor( "Directional Glow Fade: Edge Color", Color ) = ( 11.98431, 0.6901961, 0.6901961, 0 )
        _DirectionalGlowFadeWidth( "Directional Glow Fade: Width", Float ) = 0.1
        _DirectionalGlowFadeNoiseScale( "Directional Glow Fade: Noise Scale", Vector ) = ( 0.4, 0.4, 0, 0 )
        _DirectionalGlowFadeNoiseFactor( "Directional Glow Fade: Noise Factor", Float ) = 0.2
        [Toggle] _DirectionalGlowFadeInvert( "Directional Glow Fade: Invert", Float ) = 0
        [Toggle( _ENABLEDIRECTIONALDISTORTION_ON )] _EnableDirectionalDistortion( "Enable Directional Distortion", Float ) = 0
        _DirectionalDistortionFade( "Directional Distortion: Fade", Float ) = 0
        _DirectionalDistortionRotation( "Directional Distortion: Rotation", Range( 0, 360 ) ) = 0
        _DirectionalDistortionWidth( "Directional Distortion: Width", Float ) = 0.5
        _DirectionalDistortionNoiseScale( "Directional Distortion: Noise Scale", Vector ) = ( 0.4, 0.4, 0, 0 )
        _DirectionalDistortionNoiseFactor( "Directional Distortion: Noise Factor", Float ) = 0.2
        _DirectionalDistortionDistortion( "Directional Distortion: Distortion", Vector ) = ( 0, 0.1, 0, 0 )
        _DirectionalDistortionRandomDirection( "Directional Distortion: Random Direction", Range( 0, 1 ) ) = 0.1
        _DirectionalDistortionDistortionScale( "Directional Distortion: Distortion Scale", Vector ) = ( 1, 1, 0, 0 )
        [Toggle] _DirectionalDistortionInvert( "Directional Distortion: Invert", Float ) = 0
        [Toggle( _ENABLEFULLDISTORTION_ON )] _EnableFullDistortion( "Enable Full Distortion", Float ) = 0
        _FullDistortionFade( "Full Distortion: Fade", Range( 0, 1 ) ) = 1
        _FullDistortionDistortion( "Full Distortion: Distortion", Vector ) = ( 0.2, 0.2, 0, 0 )
        _FullDistortionNoiseScale( "Full Distortion: Noise Scale", Vector ) = ( 0.5, 0.5, 0, 0 )
        [Toggle( _ENABLEPIXELATE_ON )] _EnablePixelate( "Enable Pixelate", Float ) = 0
        _PixelateFade( "Pixelate: Fade", Range( 0, 1 ) ) = 1
        _PixelatePixelsPerUnit( "Pixelate: Pixels Per Unit", Float ) = 100
        _PixelatePixelDensity( "Pixelate: Pixel Density", Float ) = 16
        [Toggle( _ENABLESQUEEZE_ON )] _EnableSqueeze( "Enable Squeeze", Float ) = 0
        _SqueezeFade( "Squeeze: Fade", Range( 0, 1 ) ) = 1
        _SqueezeScale( "Squeeze: Scale", Vector ) = ( 2, 0, 0, 0 )
        _SqueezePower( "Squeeze: Power", Float ) = 1
        _SqueezeCenter( "Squeeze: Center", Vector ) = ( 0.5, 0.5, 0, 0 )
        [Toggle( _ENABLEUVDISTORT_ON )] _EnableUVDistort( "Enable UV Distort", Float ) = 0
        _UVDistortFade( "UV Distort: Fade", Range( 0, 1 ) ) = 1
        _UVDistortFrom( "UV Distort: From", Vector ) = ( -0.02, -0.02, 0, 0 )
        _UVDistortTo( "UV Distort: To", Vector ) = ( 0.02, 0.02, 0, 0 )
        _UVDistortSpeed( "UV Distort: Speed", Vector ) = ( 2, 2, 0, 0 )
        _UVDistortNoiseScale( "UV Distort: Noise Scale", Vector ) = ( 0.1, 0.1, 0, 0 )
        [Toggle( _UVDISTORTMASKTOGGLE_ON )] _UVDistortMaskToggle( "UV Distort: Mask Toggle", Float ) = 0
        [NoScaleOffset] _UVDistortMask( "UV Distort: Mask", 2D ) = "white" {}
        [Toggle( _ENABLEUVSCROLL_ON )] _EnableUVScroll( "Enable UV Scroll", Float ) = 0
        _UVScrollSpeed( "UV Scroll: Speed", Vector ) = ( 0.2, 0, 0, 0 )
        [Toggle( _ENABLEUVROTATE_ON )] _EnableUVRotate( "Enable UV Rotate", Float ) = 0
        _UVRotateSpeed( "UV Rotate: Speed", Float ) = 1
        _UVRotatePivot( "UV Rotate: Pivot", Vector ) = ( 0.5, 0.5, 0, 0 )
        [Toggle( _ENABLESINEROTATE_ON )] _EnableSineRotate( "Enable Sine Rotate", Float ) = 0
        _SineRotateFade( "Sine Rotate: Fade", Range( 0, 1 ) ) = 1
        _SineRotateAngle( "Sine Rotate: Angle", Float ) = 15
        _SineRotateFrequency( "Sine Rotate: Frequency", Float ) = 1
        _SineRotatePivot( "Sine Rotate: Pivot", Vector ) = ( 0.5, 0.5, 0, 0 )
        [Toggle( _ENABLEWIGGLE_ON )] _EnableWiggle( "Enable Wiggle", Float ) = 0
        _WiggleFade( "Wiggle: Fade", Range( 0, 1 ) ) = 1
        _WiggleSpeed( "Wiggle: Speed", Float ) = 2
        _WiggleFrequency( "Wiggle: Frequency", Float ) = 2
        _WiggleOffset( "Wiggle: Offset", Float ) = 0.02
        [Toggle( _WIGGLEFIXEDGROUNDTOGGLE_ON )] _WiggleFixedGroundToggle( "Wiggle: Fixed Ground Toggle", Float ) = 0
        [Toggle( _ENABLEUVSCALE_ON )] _EnableUVScale( "Enable UV Scale", Float ) = 0
        _UVScaleScale( "UV Scale: Scale", Vector ) = ( 1, 1, 0, 0 )
        _UVScalePivot( "UV Scale: Pivot", Vector ) = ( 0.5, 0.5, 0, 0 )
        [Toggle( _ENABLESINEMOVE_ON )] _EnableSineMove( "Enable Sine Move", Float ) = 0
        _SineMoveFade( "Sine Move: Fade", Range( 0, 1 ) ) = 1
        _SineMoveOffset( "Sine Move: Offset", Vector ) = ( 0, 0.5, 0, 0 )
        _SineMoveFrequency( "Sine Move: Frequency", Vector ) = ( 1, 1, 0, 0 )
        [Toggle( _ENABLESINESCALE_ON )] _EnableSineScale( "Enable Sine Scale", Float ) = 0
        _SineScaleFrequency( "Sine Scale: Frequency", Float ) = 2
        _SineScaleFactor( "Sine Scale: Factor", Vector ) = ( 0.2, 0.2, 0, 0 )
        [Toggle( _ENABLEVIBRATE_ON )] _EnableVibrate( "Enable Vibrate", Float ) = 0
        _VibrateFade( "Vibrate: Fade", Range( 0, 1 ) ) = 1
        _VibrateOffset( "Vibrate: Offset", Float ) = 0.04
        _VibrateFrequency( "Vibrate: Frequency", Float ) = 100
        _VibrateRotation( "Vibrate: Rotation", Float ) = 4
        [Toggle( _ENABLEWIND_ON )] _EnableWind( "Enable Wind", Float ) = 0
        _WindRotation( "Wind: Rotation", Float ) = 0
        _WindMaxRotation( "Wind: Max Rotation", Float ) = 2
        _WindRotationWindFactor( "Wind: Rotation Wind Factor", Float ) = 1
        _WindSquishFactor( "Wind: Squish Factor", Float ) = 0.3
        _WindSquishWindFactor( "Wind: Squish Wind Factor", Range( 0, 1 ) ) = 0
        [Toggle( _WINDLOCALWIND_ON )] _WindLocalWind( "Wind: Local Wind", Float ) = 0
        _WindNoiseScale( "Wind: Noise Scale", Float ) = 0.1
        _WindNoiseSpeed( "Wind: Noise Speed", Float ) = 1
        _WindMinIntensity( "Wind: Min Intensity", Float ) = -0.4
        _WindMaxIntensity( "Wind: Max Intensity", Float ) = 0.4
        [Toggle( _WINDHIGHQUALITYNOISE_ON )] _WindHighQualityNoise( "Wind: High Quality Noise", Float ) = 0
        [Toggle( _WINDISPARALLAX_ON )] _WindIsParallax( "Wind: Is Parallax", Float ) = 0
        _WindXPosition( "Wind: X Position", Float ) = 0
        _WindFlip( "Wind: Flip", Float ) = 0
        [Toggle( _ENABLESQUISH_ON )] _EnableSquish( "Enable Squish", Float ) = 0
        _SquishFade( "Squish: Fade", Range( 0, 1 ) ) = 1
        _SquishStretch( "Squish: Stretch", Float ) = 0.1
        _SquishSquish( "Squish: Squish", Float ) = 0.1
        _SquishFlip( "Squish: Flip", Range( -1, 0 ) ) = 0
        [Toggle( _ENABLECHECKERBOARD_ON )] _EnableCheckerboard( "Enable Checkerboard", Float ) = 0
        _CheckerboardDarken( "Checkerboard: Darken", Range( 0, 1 ) ) = 0.5
        _CheckerboardTiling( "Checkerboard: Tiling", Float ) = 1
        [Toggle( _ENABLEFLAME_ON )] _EnableFlame( "Enable Flame", Float ) = 0
        _FlameBrightness( "Flame: Brightness", Float ) = 10
        _FlameSmooth( "Flame: Smooth", Float ) = 2
        _FlameRadius( "Flame: Radius", Float ) = 0.2
        _FlameSpeed( "Flame: Speed", Vector ) = ( 0, -0.5, 0, 0 )
        _FlameNoiseFactor( "Flame: Noise Factor", Float ) = 2.5
        _FlameNoiseHeightFactor( "Flame: Noise Height Factor", Float ) = 1.5
        _FlameNoiseScale( "Flame: Noise Scale", Vector ) = ( 1.2, 0.8, 0, 0 )
        [Toggle( _ENABLEGAUSSIANBLUR_ON )] _EnableGaussianBlur( "Enable Gaussian Blur", Float ) = 0
        _GaussianBlurFade( "Gaussian Blur: Fade", Range( 0, 1 ) ) = 1
        _GaussianBlurOffset( "Gaussian Blur: Offset", Float ) = 0.5
        [Toggle( _ENABLESHARPEN_ON )] _EnableSharpen( "Enable Sharpen", Float ) = 0
        _SharpenFade( "Sharpen: Fade", Range( 0, 1 ) ) = 1
        _SharpenFactor( "Sharpen: Factor", Float ) = 4
        _SharpenOffset( "Sharpen: Offset", Float ) = 2
        [Toggle( _ENABLESMOOTHPIXELART_ON )] _EnableSmoothPixelArt( "Enable Smooth Pixel Art", Float ) = 0
        [Toggle( _ENABLESMOKE_ON )] _EnableSmoke( "Enable Smoke", Float ) = 0
        _SmokeAlpha( "Smoke: Alpha", Range( 0, 1 ) ) = 1
        _SmokeSmoothness( "Smoke: Smoothness", Float ) = 1
        _SmokeNoiseScale( "Smoke: Noise Scale", Float ) = 0.5
        _SmokeNoiseFactor( "Smoke: Noise Factor", Range( 0, 1 ) ) = 0.4
        _SmokeDarkEdge( "Smoke: Dark Edge", Range( 0, 1.5 ) ) = 1
        [Toggle] _SmokeVertexSeed( "Smoke: Vertex Seed", Float ) = 0
        [Toggle( _ENABLECUSTOMFADE_ON )] _EnableCustomFade( "Enable Custom Fade", Float ) = 0
        _CustomFadeFadeMask( "Custom Fade: Fade Mask", 2D ) = "white" {}
        _CustomFadeSmoothness( "Custom Fade: Smoothness", Float ) = 2
        _CustomFadeNoiseScale( "Custom Fade: Noise Scale", Vector ) = ( 1, 1, 0, 0 )
        _CustomFadeNoiseFactor( "Custom Fade: Noise Factor", Range( 0, 0.5 ) ) = 0
        _CustomFadeAlpha( "Custom Fade: Alpha", Range( 0, 1 ) ) = 1
        [Toggle( _ENABLEWORLDTILING_ON )] _EnableWorldTiling( "Enable World Tiling", Float ) = 0
        _WorldTilingScale( "World Tiling: Scale", Vector ) = ( 1, 1, 0, 0 )
        _WorldTilingOffset( "World Tiling: Offset", Vector ) = ( 0, 0, 0, 0 )
        _WorldTilingPixelsPerUnit( "World Tiling: Pixels Per Unit", Float ) = 100
        [Toggle( _ENABLESCREENTILING_ON )] _EnableScreenTiling( "Enable Screen Tiling", Float ) = 0
        _ScreenTilingScale( "Screen Tiling: Scale", Vector ) = ( 1, 1, 0, 0 )
        _ScreenTilingOffset( "Screen Tiling: Offset", Vector ) = ( 0, 0, 0, 0 )
        _ScreenTilingPixelsPerUnit( "Screen Tiling: Pixels Per Unit", Float ) = 100
        [HideInInspector] _texcoord( "", 2D ) = "white" {}

    }

    SubShader
    {
		LOD 0

        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
        	Ref [_Stencil]
        	ReadMask [_StencilReadMask]
        	WriteMask [_StencilWriteMask]
        	Comp [_StencilComp]
        	Pass [_StencilOp]
        }


        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha, One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        
        Pass
        {
            Name "Default"
        CGPROGRAM
            #define ASE_VERSION 19905

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityShaderVariables.cginc"
            #define ASE_NEEDS_TEXTURE_COORDINATES0
            #define ASE_NEEDS_VERT_TEXTURE_COORDINATES0
            #define ASE_NEEDS_VERT_POSITION
            #define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
            #define ASE_NEEDS_FRAG_POSITION
            #define ASE_NEEDS_FRAG_COLOR
            #pragma shader_feature_local _RESIZABLETEXTURE_ON
            #pragma shader_feature_local _SHADERFADING_NONE _SHADERFADING_FULL _SHADERFADING_MASK _SHADERFADING_DISSOLVE _SHADERFADING_SPREAD
            #pragma shader_feature_local _ENABLESINESCALE_ON
            #pragma shader_feature _ENABLEVIBRATE_ON
            #pragma shader_feature _ENABLESINEMOVE_ON
            #pragma shader_feature _ENABLESQUISH_ON
            #pragma shader_feature _SPRITESHEETFIX_ON
            #pragma shader_feature_local _PIXELPERFECTUV_ON
            #pragma shader_feature _ENABLEWORLDTILING_ON
            #pragma shader_feature _ENABLESCREENTILING_ON
            #pragma shader_feature _TOGGLETIMEFREQUENCY_ON
            #pragma shader_feature _TOGGLETIMEFPS_ON
            #pragma shader_feature _TOGGLETIMESPEED_ON
            #pragma shader_feature _TOGGLEUNSCALEDTIME_ON
            #pragma shader_feature _TOGGLECUSTOMTIME_ON
            #pragma shader_feature_local _SHADERSPACEPIXELATE_ON
            #pragma shader_feature _SHADERSPACE_UV _SHADERSPACE_UV_RAW _SHADERSPACE_OBJECT _SHADERSPACE_OBJECT_SCALED _SHADERSPACE_WORLD _SHADERSPACE_UI_GRAPHIC _SHADERSPACE_SCREEN
            #pragma shader_feature _PIXELPERFECTSPACE_ON
            #pragma shader_feature _BAKEDMATERIAL_ON
            #pragma shader_feature _VERTEXTINTFIRST_ON
            #pragma shader_feature _ENABLESHADOW_ON
            #pragma shader_feature _ENABLESTRONGTINT_ON
            #pragma shader_feature _ENABLEALPHATINT_ON
            #pragma shader_feature_local _ENABLEADDCOLOR_ON
            #pragma shader_feature_local _ENABLEHALFTONE_ON
            #pragma shader_feature_local _ENABLEDIRECTIONALGLOWFADE_ON
            #pragma shader_feature_local _ENABLEDIRECTIONALALPHAFADE_ON
            #pragma shader_feature_local _ENABLESOURCEGLOWDISSOLVE_ON
            #pragma shader_feature_local _ENABLESOURCEALPHADISSOLVE_ON
            #pragma shader_feature_local _ENABLEFULLGLOWDISSOLVE_ON
            #pragma shader_feature_local _ENABLEFULLALPHADISSOLVE_ON
            #pragma shader_feature_local _ENABLEDIRECTIONALDISTORTION_ON
            #pragma shader_feature_local _ENABLEFULLDISTORTION_ON
            #pragma shader_feature _ENABLETEXTURELAYER2_ON
            #pragma shader_feature _ENABLETEXTURELAYER1_ON
            #pragma shader_feature _ENABLESHIFTING_ON
            #pragma shader_feature _ENABLEENCHANTED_ON
            #pragma shader_feature_local _ENABLEPOISON_ON
            #pragma shader_feature_local _ENABLESHINE_ON
            #pragma shader_feature_local _ENABLERAINBOW_ON
            #pragma shader_feature_local _ENABLEBURN_ON
            #pragma shader_feature_local _ENABLEFROZEN_ON
            #pragma shader_feature_local _ENABLEMETAL_ON
            #pragma shader_feature_local _ENABLECAMOUFLAGE_ON
            #pragma shader_feature_local _ENABLEGLITCH_ON
            #pragma shader_feature_local _ENABLEHOLOGRAM_ON
            #pragma shader_feature _ENABLEPINGPONGGLOW_ON
            #pragma shader_feature_local _ENABLEPIXELOUTLINE_ON
            #pragma shader_feature_local _ENABLEOUTEROUTLINE_ON
            #pragma shader_feature_local _ENABLEINNEROUTLINE_ON
            #pragma shader_feature_local _ENABLESATURATION_ON
            #pragma shader_feature_local _ENABLESINEGLOW_ON
            #pragma shader_feature_local _ENABLEADDHUE_ON
            #pragma shader_feature_local _ENABLESHIFTHUE_ON
            #pragma shader_feature_local _ENABLEINKSPREAD_ON
            #pragma shader_feature_local _ENABLEBLACKTINT_ON
            #pragma shader_feature_local _ENABLESPLITTONING_ON
            #pragma shader_feature_local _ENABLEHUE_ON
            #pragma shader_feature_local _ENABLEBRIGHTNESS_ON
            #pragma shader_feature_local _ENABLECONTRAST_ON
            #pragma shader_feature _ENABLENEGATIVE_ON
            #pragma shader_feature_local _ENABLECOLORREPLACE_ON
            #pragma shader_feature_local _ENABLERECOLORRGBYCP_ON
            #pragma shader_feature _ENABLERECOLORRGB_ON
            #pragma shader_feature_local _ENABLEFLAME_ON
            #pragma shader_feature_local _ENABLECHECKERBOARD_ON
            #pragma shader_feature_local _ENABLECUSTOMFADE_ON
            #pragma shader_feature_local _ENABLESMOKE_ON
            #pragma shader_feature _ISTEXTMESHPRO_ON
            #pragma shader_feature _ISTEXT_ON
            #pragma shader_feature _ENABLESHARPEN_ON
            #pragma shader_feature _ENABLEGAUSSIANBLUR_ON
            #pragma shader_feature _ENABLESMOOTHPIXELART_ON
            #pragma shader_feature_local _TILINGFIX_ON
            #pragma shader_feature _ENABLEWIGGLE_ON
            #pragma shader_feature_local _ENABLEUVSCALE_ON
            #pragma shader_feature_local _ENABLEPIXELATE_ON
            #pragma shader_feature_local _ENABLEUVSCROLL_ON
            #pragma shader_feature_local _ENABLEUVROTATE_ON
            #pragma shader_feature_local _ENABLESINEROTATE_ON
            #pragma shader_feature_local _ENABLESQUEEZE_ON
            #pragma shader_feature_local _ENABLEUVDISTORT_ON
            #pragma shader_feature_local _ENABLEWIND_ON
            #pragma shader_feature_local _WINDLOCALWIND_ON
            #pragma shader_feature_local _WINDHIGHQUALITYNOISE_ON
            #pragma shader_feature_local _WINDISPARALLAX_ON
            #pragma shader_feature _UVDISTORTMASKTOGGLE_ON
            #pragma shader_feature _WIGGLEFIXEDGROUNDTOGGLE_ON
            #pragma shader_feature _RECOLORRGBTEXTURETOGGLE_ON
            #pragma shader_feature _RECOLORRGBYCPTEXTURETOGGLE_ON
            #pragma shader_feature_local _ADDHUEMASKTOGGLE_ON
            #pragma shader_feature_local _SINEGLOWMASKTOGGLE_ON
            #pragma shader_feature _INNEROUTLINETEXTURETOGGLE_ON
            #pragma shader_feature_local _INNEROUTLINEDISTORTIONTOGGLE_ON
            #pragma shader_feature _INNEROUTLINEOUTLINEONLYTOGGLE_ON
            #pragma shader_feature _OUTEROUTLINETEXTURETOGGLE_ON
            #pragma shader_feature _OUTEROUTLINEOUTLINEONLYTOGGLE_ON
            #pragma shader_feature_local _OUTEROUTLINEDISTORTIONTOGGLE_ON
            #pragma shader_feature _PIXELOUTLINETEXTURETOGGLE_ON
            #pragma shader_feature _PIXELOUTLINEOUTLINEONLYTOGGLE_ON
            #pragma shader_feature _CAMOUFLAGEANIMATIONTOGGLE_ON
            #pragma shader_feature _METALMASKTOGGLE_ON
            #pragma shader_feature _SHINEMASKTOGGLE_ON
            #pragma shader_feature _ENCHANTEDLERPTOGGLE_ON
            #pragma shader_feature _ENCHANTEDRAINBOWTOGGLE_ON
            #pragma shader_feature _SHIFTINGRAINBOWTOGGLE_ON
            #pragma shader_feature _TEXTURELAYER1CONTRASTTOGGLE_ON
            #pragma shader_feature _TEXTURELAYER1SHEETTOGGLE_ON
            #pragma shader_feature _TEXTURELAYER1SCROLLTOGGLE_ON
            #pragma shader_feature _TEXTURELAYER2CONTRASTTOGGLE_ON
            #pragma shader_feature _TEXTURELAYER2SHEETTOGGLE_ON
            #pragma shader_feature _TEXTURELAYER2SCROLLTOGGLE_ON
            #pragma shader_feature _ADDCOLORCONTRASTTOGGLE_ON
            #pragma shader_feature _ADDCOLORMASKTOGGLE_ON
            #pragma shader_feature _STRONGTINTCONTRASTTOGGLE_ON
            #pragma shader_feature _STRONGTINTMASKTOGGLE_ON


            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4  mask : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
                float4 ase_texcoord3 : TEXCOORD3;
                float4 ase_texcoord4 : TEXCOORD4;
                float4 ase_texcoord5 : TEXCOORD5;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float _UIMaskSoftnessX;
            float _UIMaskSoftnessY;

            uniform float _SquishStretch;
            uniform float2 _ScreenTilingScale;
            uniform float2 _ScreenTilingOffset;
            uniform float _ScreenTilingPixelsPerUnit;
            float4 _MainTex_TexelSize;
            uniform float2 _WorldTilingScale;
            uniform float2 _WorldTilingOffset;
            uniform float _WorldTilingPixelsPerUnit;
            uniform float4 _SpriteSheetRect;
            uniform float _SquishFade;
            uniform float _SquishFlip;
            uniform float _SquishSquish;
            uniform float _TimeValue;
            uniform float UnscaledTime;
            uniform float _TimeSpeed;
            uniform float _TimeFPS;
            uniform float _TimeFrequency;
            uniform float _TimeRange;
            uniform float2 _SineMoveFrequency;
            uniform float2 _SineMoveOffset;
            uniform float _SineMoveFade;
            uniform float _VibrateFrequency;
            uniform float _VibrateOffset;
            uniform float _VibrateFade;
            uniform float _VibrateRotation;
            uniform float _SineScaleFrequency;
            uniform float2 _SineScaleFactor;
            uniform float _FadingFade;
            uniform sampler2D _FadingMask;
            uniform float4 _FadingMask_ST;
            uniform float _FadingWidth;
            uniform sampler2D _UberNoiseTexture;
            uniform float2 _ShaderSpaceOffset;
            uniform float _PixelsPerUnit;
            uniform float _RectWidth;
            uniform float _RectHeight;
            uniform float _ScreenWidthUnits;
            uniform float _ShaderSpacePixelation;
            uniform float2 _FadingNoiseScale;
            uniform float2 _FadingPosition;
            uniform float _FadingNoiseFactor;
            uniform float _WindRotationWindFactor;
            uniform float WindMinIntensity;
            uniform float _WindMinIntensity;
            uniform float WindMaxIntensity;
            uniform float _WindMaxIntensity;
            uniform float _WindXPosition;
            uniform float WindNoiseScale;
            uniform float _WindNoiseScale;
            uniform float WindTime;
            uniform float _WindNoiseSpeed;
            uniform float _WindRotation;
            uniform float _WindMaxRotation;
            uniform float _WindFlip;
            uniform float _WindSquishFactor;
            uniform float _WindSquishWindFactor;
            uniform float _FullDistortionFade;
            uniform float2 _FullDistortionNoiseScale;
            uniform float2 _FullDistortionDistortion;
            uniform float2 _DirectionalDistortionDistortionScale;
            uniform float _DirectionalDistortionRandomDirection;
            uniform float2 _DirectionalDistortionDistortion;
            uniform float _DirectionalDistortionInvert;
            uniform float _DirectionalDistortionRotation;
            uniform float _DirectionalDistortionFade;
            uniform float2 _DirectionalDistortionNoiseScale;
            uniform float _DirectionalDistortionNoiseFactor;
            uniform float _DirectionalDistortionWidth;
            uniform float _HologramDistortionSpeed;
            uniform float _HologramDistortionDensity;
            uniform float _HologramDistortionScale;
            uniform float _HologramDistortionOffset;
            uniform float _HologramFade;
            uniform float2 _GlitchDistortionSpeed;
            uniform float2 _GlitchDistortionScale;
            uniform float2 _GlitchDistortion;
            uniform float2 _GlitchMaskSpeed;
            uniform float2 _GlitchMaskScale;
            uniform float _GlitchMaskMin;
            uniform float _GlitchFade;
            uniform float2 _UVDistortFrom;
            uniform float2 _UVDistortTo;
            uniform float2 _UVDistortSpeed;
            uniform float2 _UVDistortNoiseScale;
            uniform float _UVDistortFade;
            uniform sampler2D _UVDistortMask;
            uniform float4 _UVDistortMask_ST;
            uniform float2 _SqueezeCenter;
            uniform float _SqueezePower;
            uniform float2 _SqueezeScale;
            uniform float _SqueezeFade;
            uniform float _SineRotateFrequency;
            uniform float _SineRotateAngle;
            uniform float _SineRotateFade;
            uniform float2 _SineRotatePivot;
            uniform float _UVRotateSpeed;
            uniform float2 _UVRotatePivot;
            uniform float2 _UVScrollSpeed;
            uniform float _PixelatePixelDensity;
            uniform float _PixelatePixelsPerUnit;
            uniform float _PixelateFade;
            uniform float2 _UVScalePivot;
            uniform float2 _UVScaleScale;
            uniform float _WiggleFrequency;
            uniform float _WiggleSpeed;
            uniform float _WiggleOffset;
            uniform float _WiggleFade;
            uniform float _GaussianBlurOffset;
            uniform float _GaussianBlurFade;
            uniform float _SharpenOffset;
            uniform float _SharpenFactor;
            uniform float _SharpenFade;
            uniform float _TMPThickness;
            uniform float _TMPSmoothness;
            uniform int _UIVertexColorAlwaysGammaSpace;
            uniform float _SmokeVertexSeed;
            uniform float _SmokeNoiseScale;
            uniform float _SmokeNoiseFactor;
            uniform float _SmokeSmoothness;
            uniform float _SmokeDarkEdge;
            uniform float _SmokeAlpha;
            uniform sampler2D _CustomFadeFadeMask;
            uniform float2 _CustomFadeNoiseScale;
            uniform float _CustomFadeNoiseFactor;
            uniform float _CustomFadeSmoothness;
            uniform float _CustomFadeAlpha;
            uniform float _CheckerboardDarken;
            uniform float _CheckerboardTiling;
            uniform float2 _FlameSpeed;
            uniform float2 _FlameNoiseScale;
            uniform float _FlameNoiseHeightFactor;
            uniform float _FlameNoiseFactor;
            uniform float _FlameRadius;
            uniform float _FlameSmooth;
            uniform float _FlameBrightness;
            uniform float4 _RecolorRGBRedTint;
            uniform sampler2D _RecolorRGBTexture;
            uniform float4 _RecolorRGBGreenTint;
            uniform float4 _RecolorRGBBlueTint;
            uniform float _RecolorRGBFade;
            uniform sampler2D _RecolorRGBYCPTexture;
            uniform float4 _RecolorRGBYCPPurpleTint;
            uniform float4 _RecolorRGBYCPBlueTint;
            uniform float4 _RecolorRGBYCPCyanTint;
            uniform float4 _RecolorRGBYCPGreenTint;
            uniform float4 _RecolorRGBYCPYellowTint;
            uniform float4 _RecolorRGBYCPRedTint;
            uniform float _RecolorRGBYCPFade;
            uniform float4 _ColorReplaceFromColor;
            uniform float _ColorReplaceContrast;
            uniform float4 _ColorReplaceToColor;
            uniform float _ColorReplaceSmoothness;
            uniform float _ColorReplaceRange;
            uniform float _ColorReplaceFade;
            uniform float _NegativeFade;
            uniform float _Contrast;
            uniform float _Brightness;
            uniform float _Hue;
            uniform float4 _SplitToningShadowsColor;
            uniform float4 _SplitToningHighlightsColor;
            uniform float _SplitToningShift;
            uniform float _SplitToningBalance;
            uniform float _SplitToningContrast;
            uniform float _SplitToningFade;
            uniform float4 _BlackTintColor;
            uniform float _BlackTintPower;
            uniform float _BlackTintFade;
            uniform float4 _InkSpreadColor;
            uniform float _InkSpreadContrast;
            uniform float _InkSpreadFade;
            uniform float _InkSpreadDistance;
            uniform float2 _InkSpreadPosition;
            uniform float2 _InkSpreadNoiseScale;
            uniform float _InkSpreadNoiseFactor;
            uniform float _InkSpreadWidth;
            uniform float _ShiftHueSpeed;
            uniform float _AddHueSpeed;
            uniform float _AddHueSaturation;
            uniform float _AddHueBrightness;
            uniform float _AddHueContrast;
            uniform float _AddHueFade;
            uniform sampler2D _AddHueMask;
            uniform float4 _AddHueMask_ST;
            uniform float _SineGlowContrast;
            uniform float4 _SineGlowColor;
            uniform sampler2D _SineGlowMask;
            uniform float4 _SineGlowMask_ST;
            uniform float _SineGlowFade;
            uniform float _SineGlowFrequency;
            uniform float _SineGlowMax;
            uniform float _SineGlowMin;
            uniform float _Saturation;
            uniform float4 _InnerOutlineColor;
            uniform sampler2D _InnerOutlineTintTexture;
            uniform float2 _InnerOutlineTextureSpeed;
            uniform float _InnerOutlineFade;
            uniform float2 _InnerOutlineNoiseSpeed;
            uniform float2 _InnerOutlineNoiseScale;
            uniform float2 _InnerOutlineDistortionIntensity;
            uniform float _InnerOutlineWidth;
            uniform float4 _OuterOutlineColor;
            uniform sampler2D _OuterOutlineTintTexture;
            uniform float2 _OuterOutlineTextureSpeed;
            uniform float _OuterOutlineFade;
            uniform float2 _OuterOutlineNoiseSpeed;
            uniform float2 _OuterOutlineNoiseScale;
            uniform float2 _OuterOutlineDistortionIntensity;
            uniform float _OuterOutlineWidth;
            uniform float4 _PixelOutlineColor;
            uniform sampler2D _PixelOutlineTintTexture;
            uniform float2 _PixelOutlineTextureSpeed;
            uniform float _PixelOutlineFade;
            uniform float _PixelOutlineAlphaLimit;
            uniform float _PixelOutlineWidth;
            uniform float4 _PingPongGlowFrom;
            uniform float4 _PingPongGlowTo;
            uniform float _PingPongGlowFrequency;
            uniform float _PingPongGlowFade;
            uniform float _PingPongGlowContrast;
            uniform float4 _HologramTint;
            uniform float _HologramContrast;
            uniform float _HologramLineSpeed;
            uniform float _HologramLineFrequency;
            uniform float _HologramLineGap;
            uniform float _HologramMinAlpha;
            uniform float _GlitchBrightness;
            uniform float2 _GlitchNoiseSpeed;
            uniform float2 _GlitchNoiseScale;
            uniform float _GlitchHueSpeed;
            uniform float4 _CamouflageBaseColor;
            uniform float4 _CamouflageColorA;
            uniform float _CamouflageDensityA;
            uniform float2 _CamouflageDistortionSpeed;
            uniform float2 _CamouflageDistortionScale;
            uniform float2 _CamouflageDistortionIntensity;
            uniform float2 _CamouflageNoiseScaleA;
            uniform float _CamouflageSmoothnessA;
            uniform float4 _CamouflageColorB;
            uniform float _CamouflageDensityB;
            uniform float2 _CamouflageNoiseScaleB;
            uniform float _CamouflageSmoothnessB;
            uniform float _CamouflageContrast;
            uniform float _CamouflageFade;
            uniform float _MetalHighlightDensity;
            uniform float2 _MetalNoiseDistortionSpeed;
            uniform float2 _MetalNoiseDistortionScale;
            uniform float2 _MetalNoiseDistortion;
            uniform float2 _MetalNoiseSpeed;
            uniform float2 _MetalNoiseScale;
            uniform float4 _MetalHighlightColor;
            uniform float _MetalHighlightContrast;
            uniform float _MetalContrast;
            uniform float4 _MetalColor;
            uniform float _MetalFade;
            uniform sampler2D _MetalMask;
            uniform float4 _MetalMask_ST;
            uniform float _FrozenContrast;
            uniform float4 _FrozenTint;
            uniform float _FrozenSnowContrast;
            uniform float4 _FrozenSnowColor;
            uniform float _FrozenSnowDensity;
            uniform float2 _FrozenSnowScale;
            uniform float _FrozenHighlightDensity;
            uniform float2 _FrozenHighlightDistortionSpeed;
            uniform float2 _FrozenHighlightDistortionScale;
            uniform float2 _FrozenHighlightDistortion;
            uniform float2 _FrozenHighlightSpeed;
            uniform float2 _FrozenHighlightScale;
            uniform float4 _FrozenHighlightColor;
            uniform float _FrozenHighlightContrast;
            uniform float _FrozenFade;
            uniform float _BurnInsideContrast;
            uniform float4 _BurnInsideNoiseColor;
            uniform float _BurnInsideNoiseFactor;
            uniform float2 _BurnSwirlNoiseScale;
            uniform float _BurnSwirlFactor;
            uniform float2 _BurnInsideNoiseScale;
            uniform float4 _BurnInsideColor;
            uniform float _BurnRadius;
            uniform float2 _BurnPosition;
            uniform float2 _BurnEdgeNoiseScale;
            uniform float _BurnEdgeNoiseFactor;
            uniform float _BurnWidth;
            uniform float4 _BurnEdgeColor;
            uniform float _BurnFade;
            uniform float2 _RainbowCenter;
            uniform float2 _RainbowNoiseScale;
            uniform float _RainbowNoiseFactor;
            uniform float _RainbowDensity;
            uniform float _RainbowSpeed;
            uniform float _RainbowSaturation;
            uniform float _RainbowBrightness;
            uniform float _RainbowContrast;
            uniform float _RainbowFade;
            uniform float _ShineSaturation;
            uniform float _ShineContrast;
            uniform float4 _ShineColor;
            uniform float _ShineRotation;
            uniform float _ShineFrequency;
            uniform float _ShineSpeed;
            uniform float _ShineWidth;
            uniform float _ShineSmooth;
            uniform float _ShineFade;
            uniform sampler2D _ShineMask;
            uniform float4 _ShineMask_ST;
            uniform float2 _PoisonNoiseSpeed;
            uniform float2 _PoisonNoiseScale;
            uniform float _PoisonShiftSpeed;
            uniform float _PoisonDensity;
            uniform float4 _PoisonColor;
            uniform float _PoisonFade;
            uniform float _PoisonNoiseBrightness;
            uniform float _PoisonRecolorFactor;
            uniform float4 _EnchantedLowColor;
            uniform float4 _EnchantedHighColor;
            uniform float2 _EnchantedSpeed;
            uniform float2 _EnchantedScale;
            uniform float _EnchantedRainbowDensity;
            uniform float _EnchantedRainbowSpeed;
            uniform float _EnchantedRainbowSaturation;
            uniform float _EnchantedContrast;
            uniform float _EnchantedBrightness;
            uniform float _EnchantedReduce;
            uniform float _EnchantedFade;
            uniform float4 _ShiftingColorA;
            uniform float4 _ShiftingColorB;
            uniform float _ShiftingSpeed;
            uniform float _ShiftingDensity;
            uniform float _ShiftingBrightness;
            uniform float _ShiftingSaturation;
            uniform float _ShiftingContrast;
            uniform float _ShiftingFade;
            uniform sampler2D _TextureLayer1Texture;
            uniform float2 _TextureLayer1Scale;
            uniform float2 _TextureLayer1Offset;
            uniform float2 _TextureLayer1ScrollSpeed;
            uniform float _TextureLayer1EdgeClip;
            uniform int _TextureLayer1Columns;
            uniform int _TextureLayer1Rows;
            uniform float _TextureLayer1Speed;
            uniform int _TextureLayer1StartFrame;
            uniform float4 _TextureLayer1Color;
            uniform float _TextureLayer1Contrast;
            uniform float _TextureLayer1Fade;
            uniform sampler2D _TextureLayer2Texture;
            uniform float2 _TextureLayer2Scale;
            uniform float2 _TextureLayer2Offset;
            uniform float2 _TextureLayer2ScrollSpeed;
            uniform float _TextureLayer2EdgeClip;
            uniform int _TextureLayer2Columns;
            uniform int _TextureLayer2Rows;
            uniform float _TextureLayer2Speed;
            uniform int _TextureLayer2StartFrame;
            uniform float4 _TextureLayer2Color;
            uniform float _TextureLayer2Contrast;
            uniform float _TextureLayer2Fade;
            uniform float _FullAlphaDissolveFade;
            uniform float _FullAlphaDissolveWidth;
            uniform float2 _FullAlphaDissolveNoiseScale;
            uniform float4 _FullGlowDissolveEdgeColor;
            uniform float2 _FullGlowDissolveNoiseScale;
            uniform float _FullGlowDissolveFade;
            uniform float _FullGlowDissolveWidth;
            uniform float _SourceAlphaDissolveInvert;
            uniform float _SourceAlphaDissolveFade;
            uniform float2 _SourceAlphaDissolvePosition;
            uniform float2 _SourceAlphaDissolveNoiseScale;
            uniform float _SourceAlphaDissolveNoiseFactor;
            uniform float _SourceAlphaDissolveWidth;
            uniform float2 _SourceGlowDissolvePosition;
            uniform float2 _SourceGlowDissolveNoiseScale;
            uniform float _SourceGlowDissolveNoiseFactor;
            uniform float _SourceGlowDissolveFade;
            uniform float _SourceGlowDissolveWidth;
            uniform float4 _SourceGlowDissolveEdgeColor;
            uniform float _SourceGlowDissolveInvert;
            uniform float _DirectionalAlphaFadeInvert;
            uniform float _DirectionalAlphaFadeRotation;
            uniform float _DirectionalAlphaFadeFade;
            uniform float2 _DirectionalAlphaFadeNoiseScale;
            uniform float _DirectionalAlphaFadeNoiseFactor;
            uniform float _DirectionalAlphaFadeWidth;
            uniform float4 _DirectionalGlowFadeEdgeColor;
            uniform float _DirectionalGlowFadeInvert;
            uniform float _DirectionalGlowFadeRotation;
            uniform float _DirectionalGlowFadeFade;
            uniform float2 _DirectionalGlowFadeNoiseScale;
            uniform float _DirectionalGlowFadeNoiseFactor;
            uniform float _DirectionalGlowFadeWidth;
            uniform float _HalftoneInvert;
            uniform float _HalftoneTiling;
            uniform float _HalftoneFade;
            uniform float2 _HalftonePosition;
            uniform float _HalftoneFadeWidth;
            uniform float4 _AddColorColor;
            uniform sampler2D _AddColorMask;
            uniform float4 _AddColorMask_ST;
            uniform float _AddColorContrast;
            uniform float _AddColorFade;
            uniform float4 _AlphaTintColor;
            uniform float _AlphaTintMinAlpha;
            uniform float _AlphaTintFade;
            uniform float4 _StrongTintTint;
            uniform sampler2D _StrongTintMask;
            uniform float4 _StrongTintMask_ST;
            uniform float _StrongTintContrast;
            uniform float _StrongTintFade;
            uniform float4 _ShadowColor;
            uniform float _ShadowFade;
            uniform float2 _ShadowOffset;
            float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
            {
            	original -= center;
            	float C = cos( angle );
            	float S = sin( angle );
            	float t = 1 - C;
            	float m00 = t * u.x * u.x + C;
            	float m01 = t * u.x * u.y - S * u.z;
            	float m02 = t * u.x * u.z + S * u.y;
            	float m10 = t * u.x * u.y + S * u.z;
            	float m11 = t * u.y * u.y + C;
            	float m12 = t * u.y * u.z - S * u.x;
            	float m20 = t * u.x * u.z - S * u.y;
            	float m21 = t * u.y * u.z + S * u.x;
            	float m22 = t * u.z * u.z + C;
            	float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
            	return mul( finalMatrix, original ) + center;
            }
            
            float MyCustomExpression16_g13124( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13126( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float FastNoise101_g11664( float x )
            {
            	float i = floor(x);
            	float f = frac(x);
            	float s = sign(frac(x/2.0)-0.5);
            	    
            	float k = 0.5+0.5*sin(i);
            	return s*f*(f-1.0)*((16.0*k-4.0)*f*(f-1.0)-1.0);
            }
            
            float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
            float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
            float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }
            float snoise( float2 v )
            {
            	const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
            	float2 i = floor( v + dot( v, C.yy ) );
            	float2 x0 = v - i + dot( i, C.xx );
            	float2 i1;
            	i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
            	float4 x12 = x0.xyxy + C.xxzz;
            	x12.xy -= i1;
            	i = mod2D289( i );
            	float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
            	float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
            	m = m * m;
            	m = m * m;
            	float3 x = 2.0 * frac( p * C.www ) - 1.0;
            	float3 h = abs( x ) - 0.5;
            	float3 ox = floor( x + 0.5 );
            	float3 a0 = x - ox;
            	m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
            	float3 g;
            	g.x = a0.x * x0.x + h.x * x0.y;
            	g.yz = a0.yz * x12.xz + h.yz * x12.yw;
            	return 130.0 * dot( m, g );
            }
            
            float MyCustomExpression16_g13094( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13095( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13117( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13116( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13131( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13132( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13134( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13119( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13154( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float4 texturePointSmooth( sampler2D tex, float4 textureTexelSize, float2 uvs )
            {
            	float2 size;
            	size.x = textureTexelSize.z;
            	size.y = textureTexelSize.w;
            	float2 pixel = float2(1.0,1.0) / size;
            	uvs -= pixel * float2(0.5,0.5);
            	float2 uv_pixels = uvs * size;
            	float2 delta_pixel = frac(uv_pixels) - float2(0.5,0.5);
            	float2 ddxy = fwidth(uv_pixels);
            	float2 mip = log2(ddxy) - 0.5;
            	float2 clampedUV = uvs + (clamp(delta_pixel / ddxy, 0.0, 1.0) - delta_pixel) * pixel;
            	return tex2Dlod(tex, float4(clampedUV,0, min(mip.x, mip.y)));
            }
            
            float4 MyCustomExpression2_g13092( float4 InColor, int GammaSpace )
            {
            	if (GammaSpace)
            	                {
            	                    if(!IsGammaSpace())
            	                    {
            	                        InColor.rgb = UIGammaToLinear(InColor.rgb);
            	                    }
            	                }
            	return InColor;
            }
            
            float MyCustomExpression16_g13137( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13139( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13143( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float3 RGBToHSV(float3 c)
            {
            	float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            	float4 p = lerp( float4( c.bg, K.wz ), float4( c.gb, K.xy ), step( c.b, c.g ) );
            	float4 q = lerp( float4( p.xyw, c.r ), float4( c.r, p.yzx ), step( p.x, c.r ) );
            	float d = q.x - min( q.w, q.y );
            	float e = 1.0e-10;
            	return float3( abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }
            float3 MyCustomExpression115_g13161( float3 In, float3 From, float3 To, float Fuzziness, float Range )
            {
            	float Distance = distance(From, In);
            	return lerp(To, In, saturate((Distance - Range) / max(Fuzziness, 0.001)));
            }
            
            float3 HSVToRGB( float3 c )
            {
            	float4 K = float4( 1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0 );
            	float3 p = abs( frac( c.xxx + K.xyz ) * 6.0 - K.www );
            	return c.z * lerp( K.xxx, saturate( p - K.xxx ), c.y );
            }
            
            float MyCustomExpression16_g13184( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13168( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13195( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13203( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13242( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13239( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13241( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13231( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13233( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13224( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13226( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13227( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13219( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13217( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13218( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13213( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13245( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13250( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13248( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float2 MyCustomExpression10_g13260( float2 UV, int Width, int Height, float Tile, float2 Invert )
            {
            	Tile = fmod(Tile + 0.001, Width * Height);
            	float2 tileCount = float2(1, 1) / float2(Width, Height);
            	float tileY = abs(Invert.y * Height - (floor(Tile * tileCount.x) + Invert.y * 1));
            	 float tileX = abs(Invert.x * Width - ((Tile - Width * floor(Tile * tileCount.x)) + Invert.x * 1));
            	return (UV + float2(tileX, tileY)) * tileCount;
            }
            
            float2 MyCustomExpression10_g13256( float2 UV, int Width, int Height, float Tile, float2 Invert )
            {
            	Tile = fmod(Tile + 0.001, Width * Height);
            	float2 tileCount = float2(1, 1) / float2(Width, Height);
            	float tileY = abs(Invert.y * Height - (floor(Tile * tileCount.x) + Invert.y * 1));
            	 float tileX = abs(Invert.x * Width - ((Tile - Width * floor(Tile * tileCount.x)) + Invert.x * 1));
            	return (UV + float2(tileX, tileY)) * tileCount;
            }
            
            float MyCustomExpression16_g13267( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13275( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13277( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13273( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13269( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            
            float MyCustomExpression16_g13271( float linValue )
            {
            	#ifdef UNITY_COLORSPACE_GAMMA
            	return linValue;
            	#else
            	linValue = max(linValue, 0.h);
            	return max(1.055h * pow(linValue, 0.416666667h) - 0.055h, 0.h);
            	#endif
            }
            


            v2f vert(appdata_t v )
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float2 _ZeroVector = float2(0,0);
                float2 texCoord363 = v.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float4 ase_positionCS = UnityObjectToClipPos( v.vertex );
                float4 screenPos = ComputeScreenPos( ase_positionCS );
                float4 ase_positionSSNorm = screenPos / screenPos.w;
                ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
                float2 appendResult16_g11656 = (float2(_MainTex_TexelSize.x , _MainTex_TexelSize.y));
                #ifdef _ENABLESCREENTILING_ON
                float2 staticSwitch2_g11656 = ( ( ( (( ( (ase_positionSSNorm).xy * (_ScreenParams).xy ) / ( _ScreenParams.x / 10.0 ) )).xy * _ScreenTilingScale ) + _ScreenTilingOffset ) * ( _ScreenTilingPixelsPerUnit * appendResult16_g11656 ) );
                #else
                float2 staticSwitch2_g11656 = texCoord363;
                #endif
                float3 ase_positionWS = mul( unity_ObjectToWorld, float4( ( v.vertex ).xyz, 1 ) ).xyz;
                float2 appendResult16_g11657 = (float2(_MainTex_TexelSize.x , _MainTex_TexelSize.y));
                #ifdef _ENABLEWORLDTILING_ON
                float2 staticSwitch2_g11657 = ( ( ( (ase_positionWS).xy * _WorldTilingScale ) + _WorldTilingOffset ) * ( _WorldTilingPixelsPerUnit * appendResult16_g11657 ) );
                #else
                float2 staticSwitch2_g11657 = staticSwitch2_g11656;
                #endif
                float2 originalUV460 = staticSwitch2_g11657;
                float2 appendResult7_g11658 = (float2(_MainTex_TexelSize.z , _MainTex_TexelSize.w));
                #ifdef _PIXELPERFECTUV_ON
                float2 staticSwitch449 = ( floor( ( originalUV460 * appendResult7_g11658 ) ) / appendResult7_g11658 );
                #else
                float2 staticSwitch449 = originalUV460;
                #endif
                float2 uvAfterPixelArt450 = staticSwitch449;
                float2 break14_g11660 = uvAfterPixelArt450;
                float2 appendResult374 = (float2(_SpriteSheetRect.x , _SpriteSheetRect.y));
                float2 spriteRectMin376 = appendResult374;
                float2 break11_g11660 = spriteRectMin376;
                float2 appendResult375 = (float2(_SpriteSheetRect.z , _SpriteSheetRect.w));
                float2 spriteRectMax377 = appendResult375;
                float2 break10_g11660 = spriteRectMax377;
                float2 break9_g11660 = float2( 0,0 );
                float2 break8_g11660 = float2( 1,1 );
                float2 appendResult15_g11660 = (float2( (break9_g11660.x + ( break14_g11660.x - break11_g11660.x ) * ( break8_g11660.x - break9_g11660.x ) / ( break10_g11660.x - break11_g11660.x ) ) ,  (break9_g11660.y + ( break14_g11660.y - break11_g11660.y ) * ( break8_g11660.y - break9_g11660.y ) / ( break10_g11660.y - break11_g11660.y ) )));
                #ifdef _SPRITESHEETFIX_ON
                float2 staticSwitch366 = appendResult15_g11660;
                #else
                float2 staticSwitch366 = uvAfterPixelArt450;
                #endif
                float2 fixedUV475 = staticSwitch366;
                float2 break77_g13292 = fixedUV475;
                float2 appendResult72_g13292 = (float2(( _SquishStretch * ( break77_g13292.x - 0.5 ) * _SquishFade ) , ( _SquishFade * ( break77_g13292.y + _SquishFlip ) * -_SquishSquish )));
                #ifdef _ENABLESQUISH_ON
                float2 staticSwitch198 = ( appendResult72_g13292 + _ZeroVector );
                #else
                float2 staticSwitch198 = _ZeroVector;
                #endif
                float2 temp_output_2_0_g13295 = staticSwitch198;
                #ifdef _TOGGLECUSTOMTIME_ON
                float staticSwitch44_g13127 = _TimeValue;
                #else
                float staticSwitch44_g13127 = _Time.y;
                #endif
                #ifdef _TOGGLEUNSCALEDTIME_ON
                float staticSwitch34_g13127 = UnscaledTime;
                #else
                float staticSwitch34_g13127 = staticSwitch44_g13127;
                #endif
                #ifdef _TOGGLETIMESPEED_ON
                float staticSwitch37_g13127 = ( staticSwitch34_g13127 * _TimeSpeed );
                #else
                float staticSwitch37_g13127 = staticSwitch34_g13127;
                #endif
                #ifdef _TOGGLETIMEFPS_ON
                float staticSwitch38_g13127 = ( floor( ( staticSwitch37_g13127 * _TimeFPS ) ) / _TimeFPS );
                #else
                float staticSwitch38_g13127 = staticSwitch37_g13127;
                #endif
                #ifdef _TOGGLETIMEFREQUENCY_ON
                float staticSwitch42_g13127 = ( ( sin( ( staticSwitch38_g13127 * _TimeFrequency ) ) * _TimeRange ) + 100.0 );
                #else
                float staticSwitch42_g13127 = staticSwitch38_g13127;
                #endif
                float shaderTime237 = staticSwitch42_g13127;
                float temp_output_8_0_g13295 = shaderTime237;
                #ifdef _ENABLESINEMOVE_ON
                float2 staticSwitch4_g13295 = ( ( sin( ( temp_output_8_0_g13295 * _SineMoveFrequency ) ) * _SineMoveOffset * _SineMoveFade ) + temp_output_2_0_g13295 );
                #else
                float2 staticSwitch4_g13295 = temp_output_2_0_g13295;
                #endif
                float temp_output_30_0_g13296 = temp_output_8_0_g13295;
                float3 rotatedValue21_g13296 = RotateAroundAxis( float3( 0,0,0 ), float3( 0,1,0 ), float3( 0,0,1 ), ( temp_output_30_0_g13296 * _VibrateRotation ) );
                #ifdef _ENABLEVIBRATE_ON
                float2 staticSwitch6_g13295 = ( ( sin( ( _VibrateFrequency * temp_output_30_0_g13296 ) ) * _VibrateOffset * _VibrateFade * (rotatedValue21_g13296).xy ) + staticSwitch4_g13295 );
                #else
                float2 staticSwitch6_g13295 = staticSwitch4_g13295;
                #endif
                #ifdef _ENABLESINESCALE_ON
                float2 staticSwitch10_g13295 = ( staticSwitch6_g13295 + ( (v.vertex.xyz).xy * ( ( ( sin( ( _SineScaleFrequency * temp_output_8_0_g13295 ) ) + 1.0 ) * 0.5 ) * _SineScaleFactor ) ) );
                #else
                float2 staticSwitch10_g13295 = staticSwitch6_g13295;
                #endif
                float2 temp_output_424_0 = staticSwitch10_g13295;
                float2 uv_FadingMask = v.texcoord.xy * _FadingMask_ST.xy + _FadingMask_ST.zw;
                float4 tex2DNode3_g13122 = tex2Dlod( _FadingMask, float4( uv_FadingMask, 0, 0.0) );
                float temp_output_4_0_g13123 = max( _FadingWidth , 0.001 );
                float2 texCoord435 = v.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 temp_output_432_0 = (_MainTex_TexelSize).zw;
                #ifdef _PIXELPERFECTSPACE_ON
                float2 staticSwitch437 = ( floor( ( texCoord435 * temp_output_432_0 ) ) / temp_output_432_0 );
                #else
                float2 staticSwitch437 = texCoord435;
                #endif
                float2 temp_output_61_0_g13128 = staticSwitch437;
                float3 ase_objectScale = float3( length( unity_ObjectToWorld[ 0 ].xyz ), length( unity_ObjectToWorld[ 1 ].xyz ), length( unity_ObjectToWorld[ 2 ].xyz ) );
                float2 texCoord23_g13128 = v.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 appendResult28_g13128 = (float2(_RectWidth , _RectHeight));
                #if defined( _SHADERSPACE_UV )
                float2 staticSwitch1_g13128 = ( temp_output_61_0_g13128 / ( _PixelsPerUnit * (_MainTex_TexelSize).xy ) );
                #elif defined( _SHADERSPACE_UV_RAW )
                float2 staticSwitch1_g13128 = temp_output_61_0_g13128;
                #elif defined( _SHADERSPACE_OBJECT )
                float2 staticSwitch1_g13128 = (v.vertex.xyz).xy;
                #elif defined( _SHADERSPACE_OBJECT_SCALED )
                float2 staticSwitch1_g13128 = ( (v.vertex.xyz).xy * (ase_objectScale).xy );
                #elif defined( _SHADERSPACE_WORLD )
                float2 staticSwitch1_g13128 = (ase_positionWS).xy;
                #elif defined( _SHADERSPACE_UI_GRAPHIC )
                float2 staticSwitch1_g13128 = ( texCoord23_g13128 * ( appendResult28_g13128 / _PixelsPerUnit ) );
                #elif defined( _SHADERSPACE_SCREEN )
                float2 staticSwitch1_g13128 = ( ( (ase_positionSSNorm).xy * (_ScreenParams).xy ) / ( _ScreenParams.x / _ScreenWidthUnits ) );
                #else
                float2 staticSwitch1_g13128 = ( temp_output_61_0_g13128 / ( _PixelsPerUnit * (_MainTex_TexelSize).xy ) );
                #endif
                float2 temp_output_70_0_g13128 = ( _ShaderSpaceOffset + staticSwitch1_g13128 );
                #ifdef _SHADERSPACEPIXELATE_ON
                float2 staticSwitch63_g13128 = ( floor( ( temp_output_70_0_g13128 * _ShaderSpacePixelation ) ) / _ShaderSpacePixelation );
                #else
                float2 staticSwitch63_g13128 = temp_output_70_0_g13128;
                #endif
                float2 shaderPosition235 = staticSwitch63_g13128;
                float linValue16_g13124 = tex2Dlod( _UberNoiseTexture, float4( ( shaderPosition235 * _FadingNoiseScale ), 0, 0.0) ).r;
                float localMyCustomExpression16_g13124 = MyCustomExpression16_g13124( linValue16_g13124 );
                float clampResult14_g13123 = clamp( ( ( ( _FadingFade * ( 1.0 + temp_output_4_0_g13123 ) ) - localMyCustomExpression16_g13124 ) / temp_output_4_0_g13123 ) , 0.0 , 1.0 );
                float2 temp_output_27_0_g13125 = shaderPosition235;
                float linValue16_g13126 = tex2Dlod( _UberNoiseTexture, float4( ( temp_output_27_0_g13125 * _FadingNoiseScale ), 0, 0.0) ).r;
                float localMyCustomExpression16_g13126 = MyCustomExpression16_g13126( linValue16_g13126 );
                float clampResult3_g13125 = clamp( ( ( _FadingFade - ( distance( _FadingPosition , temp_output_27_0_g13125 ) + ( localMyCustomExpression16_g13126 * _FadingNoiseFactor ) ) ) / max( _FadingWidth , 0.001 ) ) , 0.0 , 1.0 );
                #if defined( _SHADERFADING_NONE )
                float staticSwitch139 = _FadingFade;
                #elif defined( _SHADERFADING_FULL )
                float staticSwitch139 = _FadingFade;
                #elif defined( _SHADERFADING_MASK )
                float staticSwitch139 = ( _FadingFade * ( tex2DNode3_g13122.r * tex2DNode3_g13122.a ) );
                #elif defined( _SHADERFADING_DISSOLVE )
                float staticSwitch139 = clampResult14_g13123;
                #elif defined( _SHADERFADING_SPREAD )
                float staticSwitch139 = clampResult3_g13125;
                #else
                float staticSwitch139 = _FadingFade;
                #endif
                float fullFade123 = staticSwitch139;
                float2 lerpResult121 = lerp( float2( 0,0 ) , temp_output_424_0 , fullFade123);
                #if defined( _SHADERFADING_NONE )
                float2 staticSwitch142 = temp_output_424_0;
                #elif defined( _SHADERFADING_FULL )
                float2 staticSwitch142 = lerpResult121;
                #elif defined( _SHADERFADING_MASK )
                float2 staticSwitch142 = lerpResult121;
                #elif defined( _SHADERFADING_DISSOLVE )
                float2 staticSwitch142 = lerpResult121;
                #elif defined( _SHADERFADING_SPREAD )
                float2 staticSwitch142 = lerpResult121;
                #else
                float2 staticSwitch142 = temp_output_424_0;
                #endif
                
                OUT.ase_texcoord3 = screenPos;
                OUT.ase_texcoord4.xyz = ase_positionWS;
                
                OUT.ase_texcoord5 = v.vertex;
                
                //setting value to unused interpolator channels and avoid initialization warnings
                OUT.ase_texcoord4.w = 0;

                v.vertex.xyz += float3( staticSwitch142 ,  0.0 );

                float4 vPosition = UnityObjectToClipPos(v.vertex);
                OUT.worldPosition = v.vertex;
                OUT.vertex = vPosition;

                float2 pixelSize = vPosition.w;
                pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

                float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
                float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
                OUT.texcoord = v.texcoord;
                OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));

                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN ) : SV_Target
            {
                //Round up the alpha color coming from the interpolator (to 1.0/256.0 steps)
                //The incoming alpha could have numerical instability, which makes it very sensible to
                //HDR color transparency blend, when it blends with the world's texture.
                const half alphaPrecision = half(0xff);
                const half invAlphaPrecision = half(1.0/alphaPrecision);
                IN.color.a = round(IN.color.a * alphaPrecision)*invAlphaPrecision;

                float2 texCoord363 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float4 screenPos = IN.ase_texcoord3;
                float4 ase_positionSSNorm = screenPos / screenPos.w;
                ase_positionSSNorm.z = ( UNITY_NEAR_CLIP_VALUE >= 0 ) ? ase_positionSSNorm.z : ase_positionSSNorm.z * 0.5 + 0.5;
                float2 appendResult16_g11656 = (float2(_MainTex_TexelSize.x , _MainTex_TexelSize.y));
                #ifdef _ENABLESCREENTILING_ON
                float2 staticSwitch2_g11656 = ( ( ( (( ( (ase_positionSSNorm).xy * (_ScreenParams).xy ) / ( _ScreenParams.x / 10.0 ) )).xy * _ScreenTilingScale ) + _ScreenTilingOffset ) * ( _ScreenTilingPixelsPerUnit * appendResult16_g11656 ) );
                #else
                float2 staticSwitch2_g11656 = texCoord363;
                #endif
                float3 ase_positionWS = IN.ase_texcoord4.xyz;
                float2 appendResult16_g11657 = (float2(_MainTex_TexelSize.x , _MainTex_TexelSize.y));
                #ifdef _ENABLEWORLDTILING_ON
                float2 staticSwitch2_g11657 = ( ( ( (ase_positionWS).xy * _WorldTilingScale ) + _WorldTilingOffset ) * ( _WorldTilingPixelsPerUnit * appendResult16_g11657 ) );
                #else
                float2 staticSwitch2_g11657 = staticSwitch2_g11656;
                #endif
                float2 originalUV460 = staticSwitch2_g11657;
                float2 appendResult7_g11658 = (float2(_MainTex_TexelSize.z , _MainTex_TexelSize.w));
                #ifdef _PIXELPERFECTUV_ON
                float2 staticSwitch449 = ( floor( ( originalUV460 * appendResult7_g11658 ) ) / appendResult7_g11658 );
                #else
                float2 staticSwitch449 = originalUV460;
                #endif
                float2 uvAfterPixelArt450 = staticSwitch449;
                float2 break14_g11660 = uvAfterPixelArt450;
                float2 appendResult374 = (float2(_SpriteSheetRect.x , _SpriteSheetRect.y));
                float2 spriteRectMin376 = appendResult374;
                float2 break11_g11660 = spriteRectMin376;
                float2 appendResult375 = (float2(_SpriteSheetRect.z , _SpriteSheetRect.w));
                float2 spriteRectMax377 = appendResult375;
                float2 break10_g11660 = spriteRectMax377;
                float2 break9_g11660 = float2( 0,0 );
                float2 break8_g11660 = float2( 1,1 );
                float2 appendResult15_g11660 = (float2( (break9_g11660.x + ( break14_g11660.x - break11_g11660.x ) * ( break8_g11660.x - break9_g11660.x ) / ( break10_g11660.x - break11_g11660.x ) ) ,  (break9_g11660.y + ( break14_g11660.y - break11_g11660.y ) * ( break8_g11660.y - break9_g11660.y ) / ( break10_g11660.y - break11_g11660.y ) )));
                #ifdef _SPRITESHEETFIX_ON
                float2 staticSwitch366 = appendResult15_g11660;
                #else
                float2 staticSwitch366 = uvAfterPixelArt450;
                #endif
                float2 fixedUV475 = staticSwitch366;
                float2 temp_output_3_0_g11663 = fixedUV475;
                #ifdef _WINDLOCALWIND_ON
                float staticSwitch117_g11664 = _WindMinIntensity;
                #else
                float staticSwitch117_g11664 = WindMinIntensity;
                #endif
                #ifdef _WINDLOCALWIND_ON
                float staticSwitch118_g11664 = _WindMaxIntensity;
                #else
                float staticSwitch118_g11664 = WindMaxIntensity;
                #endif
                float4 transform62_g11664 = mul(unity_WorldToObject,float4( 0,0,0,1 ));
                #ifdef _WINDISPARALLAX_ON
                float staticSwitch111_g11664 = _WindXPosition;
                #else
                float staticSwitch111_g11664 = transform62_g11664.x;
                #endif
                #ifdef _WINDLOCALWIND_ON
                float staticSwitch113_g11664 = _WindNoiseScale;
                #else
                float staticSwitch113_g11664 = WindNoiseScale;
                #endif
                #ifdef _TOGGLECUSTOMTIME_ON
                float staticSwitch44_g13127 = _TimeValue;
                #else
                float staticSwitch44_g13127 = _Time.y;
                #endif
                #ifdef _TOGGLEUNSCALEDTIME_ON
                float staticSwitch34_g13127 = UnscaledTime;
                #else
                float staticSwitch34_g13127 = staticSwitch44_g13127;
                #endif
                #ifdef _TOGGLETIMESPEED_ON
                float staticSwitch37_g13127 = ( staticSwitch34_g13127 * _TimeSpeed );
                #else
                float staticSwitch37_g13127 = staticSwitch34_g13127;
                #endif
                #ifdef _TOGGLETIMEFPS_ON
                float staticSwitch38_g13127 = ( floor( ( staticSwitch37_g13127 * _TimeFPS ) ) / _TimeFPS );
                #else
                float staticSwitch38_g13127 = staticSwitch37_g13127;
                #endif
                #ifdef _TOGGLETIMEFREQUENCY_ON
                float staticSwitch42_g13127 = ( ( sin( ( staticSwitch38_g13127 * _TimeFrequency ) ) * _TimeRange ) + 100.0 );
                #else
                float staticSwitch42_g13127 = staticSwitch38_g13127;
                #endif
                float shaderTime237 = staticSwitch42_g13127;
                #ifdef _WINDLOCALWIND_ON
                float staticSwitch125_g11664 = ( shaderTime237 * _WindNoiseSpeed );
                #else
                float staticSwitch125_g11664 = WindTime;
                #endif
                float temp_output_50_0_g11664 = ( ( staticSwitch111_g11664 * staticSwitch113_g11664 ) + staticSwitch125_g11664 );
                float x101_g11664 = temp_output_50_0_g11664;
                float localFastNoise101_g11664 = FastNoise101_g11664( x101_g11664 );
                float2 temp_cast_0 = (temp_output_50_0_g11664).xx;
                float simplePerlin2D121_g11664 = snoise( temp_cast_0*0.5 );
                simplePerlin2D121_g11664 = simplePerlin2D121_g11664*0.5 + 0.5;
                #ifdef _WINDHIGHQUALITYNOISE_ON
                float staticSwitch123_g11664 = simplePerlin2D121_g11664;
                #else
                float staticSwitch123_g11664 = ( localFastNoise101_g11664 + 0.5 );
                #endif
                float lerpResult86_g11664 = lerp( staticSwitch117_g11664 , staticSwitch118_g11664 , staticSwitch123_g11664);
                float clampResult29_g11664 = clamp( ( ( _WindRotationWindFactor * lerpResult86_g11664 ) + _WindRotation ) , -_WindMaxRotation , _WindMaxRotation );
                float2 temp_output_1_0_g11664 = temp_output_3_0_g11663;
                float temp_output_39_0_g11664 = ( temp_output_1_0_g11664.y + _WindFlip );
                float3 appendResult43_g11664 = (float3(0.5 , -_WindFlip , 0.0));
                float2 appendResult27_g11664 = (float2(0.0 , ( _WindSquishFactor * min( ( ( _WindSquishWindFactor * abs( lerpResult86_g11664 ) ) + abs( _WindRotation ) ) , _WindMaxRotation ) * temp_output_39_0_g11664 )));
                float3 rotatedValue19_g11664 = RotateAroundAxis( appendResult43_g11664, float3( ( appendResult27_g11664 + temp_output_1_0_g11664 ) ,  0.0 ), float3( 0,0,1 ), ( clampResult29_g11664 * temp_output_39_0_g11664 ) );
                #ifdef _ENABLEWIND_ON
                float2 staticSwitch4_g11663 = (rotatedValue19_g11664).xy;
                #else
                float2 staticSwitch4_g11663 = temp_output_3_0_g11663;
                #endif
                float2 texCoord435 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 temp_output_432_0 = (_MainTex_TexelSize).zw;
                #ifdef _PIXELPERFECTSPACE_ON
                float2 staticSwitch437 = ( floor( ( texCoord435 * temp_output_432_0 ) ) / temp_output_432_0 );
                #else
                float2 staticSwitch437 = texCoord435;
                #endif
                float2 temp_output_61_0_g13128 = staticSwitch437;
                float3 ase_objectScale = float3( length( unity_ObjectToWorld[ 0 ].xyz ), length( unity_ObjectToWorld[ 1 ].xyz ), length( unity_ObjectToWorld[ 2 ].xyz ) );
                float2 texCoord23_g13128 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 appendResult28_g13128 = (float2(_RectWidth , _RectHeight));
                #if defined( _SHADERSPACE_UV )
                float2 staticSwitch1_g13128 = ( temp_output_61_0_g13128 / ( _PixelsPerUnit * (_MainTex_TexelSize).xy ) );
                #elif defined( _SHADERSPACE_UV_RAW )
                float2 staticSwitch1_g13128 = temp_output_61_0_g13128;
                #elif defined( _SHADERSPACE_OBJECT )
                float2 staticSwitch1_g13128 = (IN.ase_texcoord5.xyz).xy;
                #elif defined( _SHADERSPACE_OBJECT_SCALED )
                float2 staticSwitch1_g13128 = ( (IN.ase_texcoord5.xyz).xy * (ase_objectScale).xy );
                #elif defined( _SHADERSPACE_WORLD )
                float2 staticSwitch1_g13128 = (ase_positionWS).xy;
                #elif defined( _SHADERSPACE_UI_GRAPHIC )
                float2 staticSwitch1_g13128 = ( texCoord23_g13128 * ( appendResult28_g13128 / _PixelsPerUnit ) );
                #elif defined( _SHADERSPACE_SCREEN )
                float2 staticSwitch1_g13128 = ( ( (ase_positionSSNorm).xy * (_ScreenParams).xy ) / ( _ScreenParams.x / _ScreenWidthUnits ) );
                #else
                float2 staticSwitch1_g13128 = ( temp_output_61_0_g13128 / ( _PixelsPerUnit * (_MainTex_TexelSize).xy ) );
                #endif
                float2 temp_output_70_0_g13128 = ( _ShaderSpaceOffset + staticSwitch1_g13128 );
                #ifdef _SHADERSPACEPIXELATE_ON
                float2 staticSwitch63_g13128 = ( floor( ( temp_output_70_0_g13128 * _ShaderSpacePixelation ) ) / _ShaderSpacePixelation );
                #else
                float2 staticSwitch63_g13128 = temp_output_70_0_g13128;
                #endif
                float2 shaderPosition235 = staticSwitch63_g13128;
                float2 temp_output_195_0_g13093 = shaderPosition235;
                float linValue16_g13094 = tex2D( _UberNoiseTexture, ( temp_output_195_0_g13093 * _FullDistortionNoiseScale ) ).r;
                float localMyCustomExpression16_g13094 = MyCustomExpression16_g13094( linValue16_g13094 );
                float linValue16_g13095 = tex2D( _UberNoiseTexture, ( ( temp_output_195_0_g13093 + float2( 0.321,0.321 ) ) * _FullDistortionNoiseScale ) ).r;
                float localMyCustomExpression16_g13095 = MyCustomExpression16_g13095( linValue16_g13095 );
                float2 appendResult189_g13093 = (float2(( localMyCustomExpression16_g13094 - 0.5 ) , ( localMyCustomExpression16_g13095 - 0.5 )));
                #ifdef _ENABLEFULLDISTORTION_ON
                float2 staticSwitch83 = ( staticSwitch4_g11663 + ( ( 1.0 - _FullDistortionFade ) * appendResult189_g13093 * _FullDistortionDistortion ) );
                #else
                float2 staticSwitch83 = staticSwitch4_g11663;
                #endif
                float2 temp_output_182_0_g13115 = shaderPosition235;
                float linValue16_g13117 = tex2D( _UberNoiseTexture, ( temp_output_182_0_g13115 * _DirectionalDistortionDistortionScale ) ).r;
                float localMyCustomExpression16_g13117 = MyCustomExpression16_g13117( linValue16_g13117 );
                float3 rotatedValue168_g13115 = RotateAroundAxis( float3( 0,0,0 ), float3( _DirectionalDistortionDistortion ,  0.0 ), float3( 0,0,1 ), ( ( ( localMyCustomExpression16_g13117 - 0.5 ) * 2.0 * _DirectionalDistortionRandomDirection ) * UNITY_PI ) );
                float3 rotatedValue136_g13115 = RotateAroundAxis( float3( 0,0,0 ), float3( temp_output_182_0_g13115 ,  0.0 ), float3( 0,0,1 ), ( ( ( _DirectionalDistortionRotation / 180.0 ) + -0.25 ) * UNITY_PI ) );
                float3 break130_g13115 = rotatedValue136_g13115;
                float linValue16_g13116 = tex2D( _UberNoiseTexture, ( temp_output_182_0_g13115 * _DirectionalDistortionNoiseScale ) ).r;
                float localMyCustomExpression16_g13116 = MyCustomExpression16_g13116( linValue16_g13116 );
                float clampResult154_g13115 = clamp( ( ( break130_g13115.x + break130_g13115.y + _DirectionalDistortionFade + ( localMyCustomExpression16_g13116 * _DirectionalDistortionNoiseFactor ) ) / max( _DirectionalDistortionWidth , 0.001 ) ) , 0.0 , 1.0 );
                #ifdef _ENABLEDIRECTIONALDISTORTION_ON
                float2 staticSwitch82 = ( staticSwitch83 + ( (rotatedValue168_g13115).xy * ( 1.0 - (( _DirectionalDistortionInvert )?( ( 1.0 - clampResult154_g13115 ) ):( clampResult154_g13115 )) ) ) );
                #else
                float2 staticSwitch82 = staticSwitch83;
                #endif
                float temp_output_81_0_g13129 = 0.5;
                float temp_output_8_0_g13129 = ( ( ( shaderTime237 * ( _HologramDistortionSpeed * temp_output_81_0_g13129 * 0.2 ) ) + ase_positionSSNorm.y ) / temp_output_81_0_g13129 );
                float2 temp_cast_4 = (temp_output_8_0_g13129).xx;
                float2 temp_cast_5 = (_HologramDistortionDensity).xx;
                float linValue16_g13131 = tex2D( _UberNoiseTexture, ( temp_cast_4 * temp_cast_5 ) ).r;
                float localMyCustomExpression16_g13131 = MyCustomExpression16_g13131( linValue16_g13131 );
                float clampResult75_g13129 = clamp( localMyCustomExpression16_g13131 , 0.075 , 0.6 );
                float2 temp_cast_6 = (temp_output_8_0_g13129).xx;
                float2 temp_cast_7 = (_HologramDistortionScale).xx;
                float linValue16_g13132 = tex2D( _UberNoiseTexture, ( temp_cast_6 * temp_cast_7 ) ).r;
                float localMyCustomExpression16_g13132 = MyCustomExpression16_g13132( linValue16_g13132 );
                float2 appendResult10_g13130 = (float2(_MainTex_TexelSize.z , _MainTex_TexelSize.w));
                float2 appendResult2_g13130 = (float2(1.0 , ( _MainTex_TexelSize.z / _MainTex_TexelSize.w )));
                #ifdef _RESIZABLETEXTURE_ON
                float2 staticSwitch9_g13130 = appendResult2_g13130;
                #else
                float2 staticSwitch9_g13130 = ( float2( 100,100 ) / appendResult10_g13130 );
                #endif
                float hologramFade182 = _HologramFade;
                float2 appendResult44_g13129 = (float2(( ( ( clampResult75_g13129 * ( localMyCustomExpression16_g13132 - 0.5 ) ) * _HologramDistortionOffset * staticSwitch9_g13130.x ) * hologramFade182 ) , 0.0));
                #ifdef _ENABLEHOLOGRAM_ON
                float2 staticSwitch59 = ( staticSwitch82 + appendResult44_g13129 );
                #else
                float2 staticSwitch59 = staticSwitch82;
                #endif
                float2 temp_output_18_0_g13118 = shaderPosition235;
                float2 glitchPosition154 = temp_output_18_0_g13118;
                float linValue16_g13134 = tex2D( _UberNoiseTexture, ( ( glitchPosition154 + ( _GlitchDistortionSpeed * shaderTime237 ) ) * _GlitchDistortionScale ) ).r;
                float localMyCustomExpression16_g13134 = MyCustomExpression16_g13134( linValue16_g13134 );
                float linValue16_g13119 = tex2D( _UberNoiseTexture, ( ( temp_output_18_0_g13118 + ( _GlitchMaskSpeed * shaderTime237 ) ) * _GlitchMaskScale ) ).r;
                float localMyCustomExpression16_g13119 = MyCustomExpression16_g13119( linValue16_g13119 );
                float glitchFade152 = ( max( localMyCustomExpression16_g13119 , _GlitchMaskMin ) * _GlitchFade );
                #ifdef _ENABLEGLITCH_ON
                float2 staticSwitch62 = ( staticSwitch59 + ( ( localMyCustomExpression16_g13134 - 0.5 ) * _GlitchDistortion * glitchFade152 ) );
                #else
                float2 staticSwitch62 = staticSwitch59;
                #endif
                float2 temp_output_1_0_g13144 = staticSwitch62;
                float2 temp_output_26_0_g13144 = shaderPosition235;
                float temp_output_25_0_g13144 = shaderTime237;
                float linValue16_g13154 = tex2D( _UberNoiseTexture, ( ( temp_output_26_0_g13144 + ( _UVDistortSpeed * temp_output_25_0_g13144 ) ) * _UVDistortNoiseScale ) ).r;
                float localMyCustomExpression16_g13154 = MyCustomExpression16_g13154( linValue16_g13154 );
                float2 lerpResult21_g13151 = lerp( _UVDistortFrom , _UVDistortTo , localMyCustomExpression16_g13154);
                float2 appendResult10_g13153 = (float2(_MainTex_TexelSize.z , _MainTex_TexelSize.w));
                float2 appendResult2_g13153 = (float2(1.0 , ( _MainTex_TexelSize.z / _MainTex_TexelSize.w )));
                #ifdef _RESIZABLETEXTURE_ON
                float2 staticSwitch9_g13153 = appendResult2_g13153;
                #else
                float2 staticSwitch9_g13153 = ( float2( 100,100 ) / appendResult10_g13153 );
                #endif
                float2 uv_UVDistortMask = IN.texcoord.xy * _UVDistortMask_ST.xy + _UVDistortMask_ST.zw;
                float4 tex2DNode3_g13152 = tex2D( _UVDistortMask, uv_UVDistortMask );
                #ifdef _UVDISTORTMASKTOGGLE_ON
                float staticSwitch29_g13151 = ( _UVDistortFade * ( tex2DNode3_g13152.r * tex2DNode3_g13152.a ) );
                #else
                float staticSwitch29_g13151 = _UVDistortFade;
                #endif
                #ifdef _ENABLEUVDISTORT_ON
                float2 staticSwitch5_g13144 = ( temp_output_1_0_g13144 + ( lerpResult21_g13151 * staticSwitch9_g13153 * staticSwitch29_g13151 ) );
                #else
                float2 staticSwitch5_g13144 = temp_output_1_0_g13144;
                #endif
                float2 temp_output_1_0_g13150 = staticSwitch5_g13144;
                #ifdef _ENABLESQUEEZE_ON
                float2 staticSwitch7_g13144 = ( temp_output_1_0_g13150 + ( ( temp_output_1_0_g13150 - _SqueezeCenter ) * pow( distance( temp_output_1_0_g13150 , _SqueezeCenter ) , _SqueezePower ) * _SqueezeScale * _SqueezeFade ) );
                #else
                float2 staticSwitch7_g13144 = staticSwitch5_g13144;
                #endif
                float3 rotatedValue36_g13149 = RotateAroundAxis( float3( _SineRotatePivot ,  0.0 ), float3( staticSwitch7_g13144 ,  0.0 ), float3( 0,0,1 ), ( sin( ( temp_output_25_0_g13144 * _SineRotateFrequency ) ) * ( ( _SineRotateAngle / 360.0 ) * UNITY_PI ) * _SineRotateFade ) );
                #ifdef _ENABLESINEROTATE_ON
                float2 staticSwitch9_g13144 = (rotatedValue36_g13149).xy;
                #else
                float2 staticSwitch9_g13144 = staticSwitch7_g13144;
                #endif
                float3 rotatedValue8_g13148 = RotateAroundAxis( float3( _UVRotatePivot ,  0.0 ), float3( staticSwitch9_g13144 ,  0.0 ), float3( 0,0,1 ), ( temp_output_25_0_g13144 * _UVRotateSpeed * UNITY_PI ) );
                #ifdef _ENABLEUVROTATE_ON
                float2 staticSwitch16_g13144 = (rotatedValue8_g13148).xy;
                #else
                float2 staticSwitch16_g13144 = staticSwitch9_g13144;
                #endif
                #ifdef _ENABLEUVSCROLL_ON
                float2 staticSwitch14_g13144 = ( ( _UVScrollSpeed * temp_output_25_0_g13144 ) + staticSwitch16_g13144 );
                #else
                float2 staticSwitch14_g13144 = staticSwitch16_g13144;
                #endif
                float2 appendResult35_g13146 = (float2(_MainTex_TexelSize.z , _MainTex_TexelSize.w));
                float2 MultFactor30_g13146 = ( ( _PixelatePixelDensity * ( appendResult35_g13146 / _PixelatePixelsPerUnit ) ) * ( 1.0 / max( _PixelateFade , 1E-05 ) ) );
                float2 clampResult46_g13146 = clamp( ( floor( ( MultFactor30_g13146 * ( staticSwitch14_g13144 + ( float2( 0.5,0.5 ) / MultFactor30_g13146 ) ) ) ) / MultFactor30_g13146 ) , float2( 0,0 ) , float2( 1,1 ) );
                #ifdef _ENABLEPIXELATE_ON
                float2 staticSwitch4_g13144 = clampResult46_g13146;
                #else
                float2 staticSwitch4_g13144 = staticSwitch14_g13144;
                #endif
                #ifdef _ENABLEUVSCALE_ON
                float2 staticSwitch24_g13144 = ( ( ( staticSwitch4_g13144 - _UVScalePivot ) / _UVScaleScale ) + _UVScalePivot );
                #else
                float2 staticSwitch24_g13144 = staticSwitch4_g13144;
                #endif
                float2 temp_output_1_0_g13155 = staticSwitch24_g13144;
                float temp_output_7_0_g13155 = ( sin( ( _WiggleFrequency * ( temp_output_26_0_g13144.y + ( _WiggleSpeed * temp_output_25_0_g13144 ) ) ) ) * _WiggleOffset * _WiggleFade );
                #ifdef _WIGGLEFIXEDGROUNDTOGGLE_ON
                float staticSwitch18_g13155 = ( temp_output_7_0_g13155 * temp_output_1_0_g13155.y );
                #else
                float staticSwitch18_g13155 = temp_output_7_0_g13155;
                #endif
                float2 appendResult12_g13155 = (float2(staticSwitch18_g13155 , 0.0));
                #ifdef _ENABLEWIGGLE_ON
                float2 staticSwitch13_g13155 = ( temp_output_1_0_g13155 + appendResult12_g13155 );
                #else
                float2 staticSwitch13_g13155 = temp_output_1_0_g13155;
                #endif
                float2 temp_output_484_0 = staticSwitch13_g13155;
                float2 texCoord131 = IN.texcoord.xy * float2( 1,1 ) + float2( 0,0 );
                float2 uv_FadingMask = IN.texcoord.xy * _FadingMask_ST.xy + _FadingMask_ST.zw;
                float4 tex2DNode3_g13122 = tex2D( _FadingMask, uv_FadingMask );
                float temp_output_4_0_g13123 = max( _FadingWidth , 0.001 );
                float linValue16_g13124 = tex2D( _UberNoiseTexture, ( shaderPosition235 * _FadingNoiseScale ) ).r;
                float localMyCustomExpression16_g13124 = MyCustomExpression16_g13124( linValue16_g13124 );
                float clampResult14_g13123 = clamp( ( ( ( _FadingFade * ( 1.0 + temp_output_4_0_g13123 ) ) - localMyCustomExpression16_g13124 ) / temp_output_4_0_g13123 ) , 0.0 , 1.0 );
                float2 temp_output_27_0_g13125 = shaderPosition235;
                float linValue16_g13126 = tex2D( _UberNoiseTexture, ( temp_output_27_0_g13125 * _FadingNoiseScale ) ).r;
                float localMyCustomExpression16_g13126 = MyCustomExpression16_g13126( linValue16_g13126 );
                float clampResult3_g13125 = clamp( ( ( _FadingFade - ( distance( _FadingPosition , temp_output_27_0_g13125 ) + ( localMyCustomExpression16_g13126 * _FadingNoiseFactor ) ) ) / max( _FadingWidth , 0.001 ) ) , 0.0 , 1.0 );
                #if defined( _SHADERFADING_NONE )
                float staticSwitch139 = _FadingFade;
                #elif defined( _SHADERFADING_FULL )
                float staticSwitch139 = _FadingFade;
                #elif defined( _SHADERFADING_MASK )
                float staticSwitch139 = ( _FadingFade * ( tex2DNode3_g13122.r * tex2DNode3_g13122.a ) );
                #elif defined( _SHADERFADING_DISSOLVE )
                float staticSwitch139 = clampResult14_g13123;
                #elif defined( _SHADERFADING_SPREAD )
                float staticSwitch139 = clampResult3_g13125;
                #else
                float staticSwitch139 = _FadingFade;
                #endif
                float fullFade123 = staticSwitch139;
                float2 lerpResult130 = lerp( texCoord131 , temp_output_484_0 , fullFade123);
                #if defined( _SHADERFADING_NONE )
                float2 staticSwitch145 = temp_output_484_0;
                #elif defined( _SHADERFADING_FULL )
                float2 staticSwitch145 = lerpResult130;
                #elif defined( _SHADERFADING_MASK )
                float2 staticSwitch145 = lerpResult130;
                #elif defined( _SHADERFADING_DISSOLVE )
                float2 staticSwitch145 = lerpResult130;
                #elif defined( _SHADERFADING_SPREAD )
                float2 staticSwitch145 = lerpResult130;
                #else
                float2 staticSwitch145 = temp_output_484_0;
                #endif
                #ifdef _TILINGFIX_ON
                float2 staticSwitch485 = ( ( ( staticSwitch145 % float2( 1,1 ) ) + float2( 1,1 ) ) % float2( 1,1 ) );
                #else
                float2 staticSwitch485 = staticSwitch145;
                #endif
                float2 break14_g13156 = staticSwitch485;
                float2 break11_g13156 = float2( 0,0 );
                float2 break10_g13156 = float2( 1,1 );
                float2 break9_g13156 = spriteRectMin376;
                float2 break8_g13156 = spriteRectMax377;
                float2 appendResult15_g13156 = (float2( (break9_g13156.x + ( break14_g13156.x - break11_g13156.x ) * ( break8_g13156.x - break9_g13156.x ) / ( break10_g13156.x - break11_g13156.x ) ) ,  (break9_g13156.y + ( break14_g13156.y - break11_g13156.y ) * ( break8_g13156.y - break9_g13156.y ) / ( break10_g13156.y - break11_g13156.y ) )));
                #ifdef _SPRITESHEETFIX_ON
                float2 staticSwitch371 = min( max( appendResult15_g13156 , spriteRectMin376 ) , spriteRectMax377 );
                #else
                float2 staticSwitch371 = staticSwitch485;
                #endif
                float2 appendResult7_g13206 = (float2(_MainTex_TexelSize.z , _MainTex_TexelSize.w));
                #ifdef _PIXELPERFECTUV_ON
                float2 staticSwitch427 = ( originalUV460 + ( floor( ( ( staticSwitch371 - uvAfterPixelArt450 ) * appendResult7_g13206 ) ) / appendResult7_g13206 ) );
                #else
                float2 staticSwitch427 = staticSwitch371;
                #endif
                float2 finalUV146 = staticSwitch427;
                float2 temp_output_1_0_g13096 = finalUV146;
                sampler2D tex3_g13097 = _MainTex;
                float4 textureTexelSize3_g13097 = _MainTex_TexelSize;
                float2 uvs3_g13097 = temp_output_1_0_g13096;
                float4 localtexturePointSmooth3_g13097 = texturePointSmooth( tex3_g13097 , textureTexelSize3_g13097 , uvs3_g13097 );
                #ifdef _ENABLESMOOTHPIXELART_ON
                float4 staticSwitch8_g13096 = localtexturePointSmooth3_g13097;
                #else
                float4 staticSwitch8_g13096 = tex2D( _MainTex, temp_output_1_0_g13096 );
                #endif
                float temp_output_10_0_g13098 = ( _GaussianBlurOffset * _GaussianBlurFade * 0.005 );
                float temp_output_2_0_g13108 = temp_output_10_0_g13098;
                float2 appendResult16_g13108 = (float2(temp_output_2_0_g13108 , 0.0));
                float2 appendResult25_g13110 = (float2(_MainTex_TexelSize.x , _MainTex_TexelSize.y));
                float2 temp_output_26_0_g13110 = ( appendResult16_g13108 * appendResult25_g13110 );
                float2 temp_output_7_0_g13098 = temp_output_1_0_g13096;
                float2 temp_output_1_0_g13108 = ( temp_output_7_0_g13098 + ( temp_output_10_0_g13098 * float2( 1,1 ) ) );
                float2 temp_output_1_0_g13110 = temp_output_1_0_g13108;
                float2 appendResult17_g13108 = (float2(0.0 , temp_output_2_0_g13108));
                float2 appendResult25_g13109 = (float2(_MainTex_TexelSize.x , _MainTex_TexelSize.y));
                float2 temp_output_26_0_g13109 = ( appendResult17_g13108 * appendResult25_g13109 );
                float2 temp_output_1_0_g13109 = temp_output_1_0_g13108;
                float temp_output_2_0_g13099 = temp_output_10_0_g13098;
                float2 appendResult16_g13099 = (float2(temp_output_2_0_g13099 , 0.0));
                float2 appendResult25_g13101 = (float2(_MainTex_TexelSize.x , _MainTex_TexelSize.y));
                float2 temp_output_26_0_g13101 = ( appendResult16_g13099 * appendResult25_g13101 );
                float2 temp_output_1_0_g13099 = ( temp_output_7_0_g13098 + ( temp_output_10_0_g13098 * float2( -1,1 ) ) );
                float2 temp_output_1_0_g13101 = temp_output_1_0_g13099;
                float2 appendResult17_g13099 = (float2(0.0 , temp_output_2_0_g13099));
                float2 appendResult25_g13100 = (float2(_MainTex_TexelSize.x , _MainTex_TexelSize.y));
                float2 temp_output_26_0_g13100 = ( appendResult17_g13099 * appendResult25_g13100 );
                float2 temp_output_1_0_g13100 = temp_output_1_0_g13099;
                float temp_output_2_0_g13105 = temp_output_10_0_g13098;
                float2 appendResult16_g13105 = (float2(temp_output_2_0_g13105 , 0.0));
                float2 appendResult25_g13107 = (float2(_MainTex_TexelSize.x , _MainTex_TexelSize.y));
                float2 temp_output_26_0_g13107 = ( appendResult16_g13105 * appendResult25_g13107 );
                float2 temp_output_1_0_g13105 = ( temp_output_7_0_g13098 + ( temp_output_10_0_g13098 * float2( -1,-1 ) ) );
                float2 temp_output_1_0_g13107 = temp_output_1_0_g13105;
                float2 appendResult17_g13105 = (float2(0.0 , temp_output_2_0_g13105));
                float2 appendResult25_g13106 = (float2(_MainTex_TexelSize.x , _MainTex_TexelSize.y));
                float2 temp_output_26_0_g13106 = ( appendResult17_g13105 * appendResult25_g13106 );
                float2 temp_output_1_0_g13106 = temp_output_1_0_g13105;
                float temp_output_2_0_g13102 = temp_output_10_0_g13098;
                float2 appendResult16_g13102 = (float2(temp_output_2_0_g13102 , 0.0));
                float2 appendResult25_g13104 = (float2(_MainTex_TexelSize.x , _MainTex_TexelSize.y));
                float2 temp_output_26_0_g13104 = ( appendResult16_g13102 * appendResult25_g13104 );
                float2 temp_output_1_0_g13102 = ( temp_output_7_0_g13098 + ( temp_output_10_0_g13098 * float2( 1,-1 ) ) );
                float2 temp_output_1_0_g13104 = temp_output_1_0_g13102;
                float2 appendResult17_g13102 = (float2(0.0 , temp_output_2_0_g13102));
                float2 appendResult25_g13103 = (float2(_MainTex_TexelSize.x , _MainTex_TexelSize.y));
                float2 temp_output_26_0_g13103 = ( appendResult17_g13102 * appendResult25_g13103 );
                float2 temp_output_1_0_g13103 = temp_output_1_0_g13102;
                #ifdef _ENABLEGAUSSIANBLUR_ON
                float4 staticSwitch3_g13096 = ( ( ( ( tex2D( _MainTex, ( temp_output_26_0_g13110 + temp_output_1_0_g13110 ) ) + tex2D( _MainTex, ( -temp_output_26_0_g13110 + temp_output_1_0_g13110 ) ) ) + ( tex2D( _MainTex, ( temp_output_26_0_g13109 + temp_output_1_0_g13109 ) ) + tex2D( _MainTex, ( -temp_output_26_0_g13109 + temp_output_1_0_g13109 ) ) ) ) + ( ( tex2D( _MainTex, ( temp_output_26_0_g13101 + temp_output_1_0_g13101 ) ) + tex2D( _MainTex, ( -temp_output_26_0_g13101 + temp_output_1_0_g13101 ) ) ) + ( tex2D( _MainTex, ( temp_output_26_0_g13100 + temp_output_1_0_g13100 ) ) + tex2D( _MainTex, ( -temp_output_26_0_g13100 + temp_output_1_0_g13100 ) ) ) ) + ( ( tex2D( _MainTex, ( temp_output_26_0_g13107 + temp_output_1_0_g13107 ) ) + tex2D( _MainTex, ( -temp_output_26_0_g13107 + temp_output_1_0_g13107 ) ) ) + ( tex2D( _MainTex, ( temp_output_26_0_g13106 + temp_output_1_0_g13106 ) ) + tex2D( _MainTex, ( -temp_output_26_0_g13106 + temp_output_1_0_g13106 ) ) ) ) + ( ( tex2D( _MainTex, ( temp_output_26_0_g13104 + temp_output_1_0_g13104 ) ) + tex2D( _MainTex, ( -temp_output_26_0_g13104 + temp_output_1_0_g13104 ) ) ) + ( tex2D( _MainTex, ( temp_output_26_0_g13103 + temp_output_1_0_g13103 ) ) + tex2D( _MainTex, ( -temp_output_26_0_g13103 + temp_output_1_0_g13103 ) ) ) ) ) * 0.0625 );
                #else
                float4 staticSwitch3_g13096 = staticSwitch8_g13096;
                #endif
                float2 temp_output_1_0_g13111 = temp_output_1_0_g13096;
                float4 tex2DNode4_g13111 = tex2D( _MainTex, temp_output_1_0_g13111 );
                float temp_output_2_0_g13112 = _SharpenOffset;
                float2 appendResult16_g13112 = (float2(temp_output_2_0_g13112 , 0.0));
                float2 appendResult25_g13114 = (float2(_MainTex_TexelSize.x , _MainTex_TexelSize.y));
                float2 temp_output_26_0_g13114 = ( appendResult16_g13112 * appendResult25_g13114 );
                float2 temp_output_1_0_g13112 = temp_output_1_0_g13111;
                float2 temp_output_1_0_g13114 = temp_output_1_0_g13112;
                float2 appendResult17_g13112 = (float2(0.0 , temp_output_2_0_g13112));
                float2 appendResult25_g13113 = (float2(_MainTex_TexelSize.x , _MainTex_TexelSize.y));
                float2 temp_output_26_0_g13113 = ( appendResult17_g13112 * appendResult25_g13113 );
                float2 temp_output_1_0_g13113 = temp_output_1_0_g13112;
                float4 break22_g13111 = ( tex2DNode4_g13111 - ( ( ( ( ( tex2D( _MainTex, ( temp_output_26_0_g13114 + temp_output_1_0_g13114 ) ) + tex2D( _MainTex, ( -temp_output_26_0_g13114 + temp_output_1_0_g13114 ) ) ) + ( tex2D( _MainTex, ( temp_output_26_0_g13113 + temp_output_1_0_g13113 ) ) + tex2D( _MainTex, ( -temp_output_26_0_g13113 + temp_output_1_0_g13113 ) ) ) ) / 4.0 ) - tex2DNode4_g13111 ) * ( _SharpenFactor * _SharpenFade ) ) );
                float clampResult23_g13111 = clamp( break22_g13111.a , 0.0 , 1.0 );
                float4 appendResult24_g13111 = (float4(break22_g13111.r , break22_g13111.g , break22_g13111.b , clampResult23_g13111));
                #ifdef _ENABLESHARPEN_ON
                float4 staticSwitch12_g13096 = appendResult24_g13111;
                #else
                float4 staticSwitch12_g13096 = staticSwitch3_g13096;
                #endif
                float4 temp_output_1_0_g13120 = staticSwitch12_g13096;
                float4 appendResult6_g13120 = (float4(1.0 , 1.0 , 1.0 , temp_output_1_0_g13120.a));
                #ifdef _ISTEXT_ON
                float4 staticSwitch2_g13120 = appendResult6_g13120;
                #else
                float4 staticSwitch2_g13120 = temp_output_1_0_g13120;
                #endif
                float4 break8_g13120 = staticSwitch2_g13120;
                float clampResult14_g13120 = clamp( ( ( break8_g13120.a + _TMPThickness ) * ( _TMPSmoothness / ( 1.0 + _TMPThickness ) ) ) , 0.0 , 1.0 );
                float4 appendResult13_g13120 = (float4(break8_g13120.r , break8_g13120.g , break8_g13120.b , clampResult14_g13120));
                #ifdef _ISTEXTMESHPRO_ON
                float4 staticSwitch3_g13120 = appendResult13_g13120;
                #else
                float4 staticSwitch3_g13120 = staticSwitch2_g13120;
                #endif
                float4 temp_output_494_0 = staticSwitch3_g13120;
                float4 temp_output_1_0_g13121 = temp_output_494_0;
                float4 InColor2_g13092 = IN.color;
                int GammaSpace2_g13092 = _UIVertexColorAlwaysGammaSpace;
                float4 localMyCustomExpression2_g13092 = MyCustomExpression2_g13092( InColor2_g13092 , GammaSpace2_g13092 );
                float4 UIVertexColor514 = localMyCustomExpression2_g13092;
                float4 appendResult8_g13121 = (float4(( (temp_output_1_0_g13121).rgb * (UIVertexColor514).rgb ) , temp_output_1_0_g13121.a));
                #ifdef _VERTEXTINTFIRST_ON
                float4 staticSwitch354 = appendResult8_g13121;
                #else
                float4 staticSwitch354 = temp_output_494_0;
                #endif
                float4 originalColor191 = staticSwitch354;
                float4 temp_output_1_0_g13135 = originalColor191;
                float4 temp_output_1_0_g13136 = temp_output_1_0_g13135;
                float2 temp_output_7_0_g13135 = finalUV146;
                float2 temp_output_43_0_g13136 = temp_output_7_0_g13135;
                float2 temp_cast_19 = (_SmokeNoiseScale).xx;
                float linValue16_g13137 = tex2D( _UberNoiseTexture, ( ( ( IN.color.r * (( _SmokeVertexSeed )?( 5.0 ):( 0.0 )) ) + temp_output_43_0_g13136 ) * temp_cast_19 ) ).r;
                float localMyCustomExpression16_g13137 = MyCustomExpression16_g13137( linValue16_g13137 );
                float clampResult28_g13136 = clamp( ( ( ( localMyCustomExpression16_g13137 - 1.0 ) * _SmokeNoiseFactor ) + ( ( ( IN.color.a / 2.5 ) - distance( temp_output_43_0_g13136 , float2( 0.5,0.5 ) ) ) * 2.5 * _SmokeSmoothness ) ) , 0.0 , 1.0 );
                float3 lerpResult34_g13136 = lerp( (temp_output_1_0_g13136).rgb , float3( 0,0,0 ) , ( ( 1.0 - clampResult28_g13136 ) * _SmokeDarkEdge ));
                float4 appendResult31_g13136 = (float4(lerpResult34_g13136 , ( clampResult28_g13136 * _SmokeAlpha * temp_output_1_0_g13136.a )));
                #ifdef _ENABLESMOKE_ON
                float4 staticSwitch2_g13135 = appendResult31_g13136;
                #else
                float4 staticSwitch2_g13135 = temp_output_1_0_g13135;
                #endif
                float4 temp_output_1_0_g13138 = staticSwitch2_g13135;
                float2 temp_output_57_0_g13138 = temp_output_7_0_g13135;
                float4 tex2DNode3_g13138 = tex2D( _CustomFadeFadeMask, temp_output_57_0_g13138 );
                float linValue16_g13139 = tex2D( _UberNoiseTexture, ( temp_output_57_0_g13138 * _CustomFadeNoiseScale ) ).r;
                float localMyCustomExpression16_g13139 = MyCustomExpression16_g13139( linValue16_g13139 );
                float clampResult37_g13138 = clamp( ( ( ( IN.color.a * 2.0 ) - 1.0 ) + ( tex2DNode3_g13138.r + ( localMyCustomExpression16_g13139 * _CustomFadeNoiseFactor ) ) ) , 0.0 , 1.0 );
                float4 appendResult13_g13138 = (float4((temp_output_1_0_g13138).rgb , ( temp_output_1_0_g13138.a * pow( clampResult37_g13138 , ( _CustomFadeSmoothness / max( tex2DNode3_g13138.r , 0.05 ) ) ) * _CustomFadeAlpha )));
                #ifdef _ENABLECUSTOMFADE_ON
                float4 staticSwitch3_g13135 = appendResult13_g13138;
                #else
                float4 staticSwitch3_g13135 = staticSwitch2_g13135;
                #endif
                float4 temp_output_1_0_g13140 = staticSwitch3_g13135;
                float4 temp_output_1_0_g13141 = temp_output_1_0_g13140;
                float2 appendResult4_g13141 = (float2(ase_positionWS.x , ase_positionWS.y));
                float2 temp_output_44_0_g13141 = ( appendResult4_g13141 * _CheckerboardTiling * 0.5 );
                float2 break12_g13141 = step( ( ceil( temp_output_44_0_g13141 ) - temp_output_44_0_g13141 ) , float2( 0.5,0.5 ) );
                float4 appendResult42_g13141 = (float4(( (temp_output_1_0_g13141).rgb * min( ( _CheckerboardDarken + abs( ( -break12_g13141.x + break12_g13141.y ) ) ) , 1.0 ) ) , temp_output_1_0_g13141.a));
                #ifdef _ENABLECHECKERBOARD_ON
                float4 staticSwitch2_g13140 = appendResult42_g13141;
                #else
                float4 staticSwitch2_g13140 = temp_output_1_0_g13140;
                #endif
                float2 temp_output_75_0_g13142 = finalUV146;
                float linValue16_g13143 = tex2D( _UberNoiseTexture, ( ( ( shaderTime237 * _FlameSpeed ) + temp_output_75_0_g13142 ) * _FlameNoiseScale ) ).r;
                float localMyCustomExpression16_g13143 = MyCustomExpression16_g13143( linValue16_g13143 );
                float saferPower57_g13142 = abs( max( ( temp_output_75_0_g13142.y - 0.2 ) , 0.0 ) );
                float temp_output_47_0_g13142 = max( _FlameRadius , 0.01 );
                float clampResult70_g13142 = clamp( ( ( ( localMyCustomExpression16_g13143 * pow( saferPower57_g13142 , _FlameNoiseHeightFactor ) * _FlameNoiseFactor ) + ( ( temp_output_47_0_g13142 - distance( temp_output_75_0_g13142 , float2( 0.5,0.4 ) ) ) / temp_output_47_0_g13142 ) ) * _FlameSmooth ) , 0.0 , 1.0 );
                float temp_output_63_0_g13142 = ( clampResult70_g13142 * _FlameBrightness );
                float4 appendResult31_g13142 = (float4(temp_output_63_0_g13142 , temp_output_63_0_g13142 , temp_output_63_0_g13142 , clampResult70_g13142));
                #ifdef _ENABLEFLAME_ON
                float4 staticSwitch6_g13140 = ( appendResult31_g13142 * staticSwitch2_g13140 );
                #else
                float4 staticSwitch6_g13140 = staticSwitch2_g13140;
                #endif
                float4 temp_output_3_0_g13157 = staticSwitch6_g13140;
                float4 temp_output_1_0_g13189 = temp_output_3_0_g13157;
                float4 appendResult91_g13189 = (float4((temp_output_1_0_g13189).rgb , 1.0));
                float2 temp_output_1_0_g13157 = finalUV146;
                #ifdef _RECOLORRGBTEXTURETOGGLE_ON
                float4 staticSwitch81_g13189 = tex2D( _RecolorRGBTexture, temp_output_1_0_g13157 );
                #else
                float4 staticSwitch81_g13189 = appendResult91_g13189;
                #endif
                float3 break82_g13189 = (staticSwitch81_g13189).xyz;
                float temp_output_84_0_g13189 = max( ( break82_g13189.x + break82_g13189.y + break82_g13189.z ) , 0.001 );
                float4 break71_g13189 = ( ( _RecolorRGBRedTint * ( break82_g13189.x / temp_output_84_0_g13189 ) ) + ( _RecolorRGBGreenTint * ( break82_g13189.y / temp_output_84_0_g13189 ) ) + ( ( break82_g13189.z / temp_output_84_0_g13189 ) * _RecolorRGBBlueTint ) );
                float3 appendResult56_g13189 = (float3(break71_g13189.r , break71_g13189.g , break71_g13189.b));
                float4 break2_g13190 = temp_output_1_0_g13189;
                float saferPower57_g13189 = abs( ( ( break2_g13190.x + break2_g13190.x + break2_g13190.y + break2_g13190.y + break2_g13190.y + break2_g13190.z ) / 6.0 ) );
                float3 lerpResult26_g13189 = lerp( (temp_output_1_0_g13189).rgb , ( appendResult56_g13189 * pow( saferPower57_g13189 , ( max( break71_g13189.a , 0.01 ) * 2.0 ) ) ) , ( staticSwitch81_g13189.w * _RecolorRGBFade ));
                float4 appendResult30_g13189 = (float4(lerpResult26_g13189 , temp_output_1_0_g13189.a));
                #ifdef _ENABLERECOLORRGB_ON
                float4 staticSwitch43_g13157 = appendResult30_g13189;
                #else
                float4 staticSwitch43_g13157 = temp_output_3_0_g13157;
                #endif
                float4 temp_output_1_0_g13187 = staticSwitch43_g13157;
                #ifdef _RECOLORRGBYCPTEXTURETOGGLE_ON
                float4 staticSwitch62_g13187 = tex2D( _RecolorRGBYCPTexture, temp_output_1_0_g13157 );
                #else
                float4 staticSwitch62_g13187 = temp_output_1_0_g13187;
                #endif
                float3 hsvTorgb33_g13187 = RGBToHSV( staticSwitch62_g13187.rgb );
                float temp_output_43_0_g13187 = ( ( hsvTorgb33_g13187.x + 0.08333334 ) % 1.0 );
                float4 ifLocalVar46_g13187 = 0;
                if( temp_output_43_0_g13187 >= 0.8333333 )
                ifLocalVar46_g13187 = _RecolorRGBYCPPurpleTint;
                else
                ifLocalVar46_g13187 = _RecolorRGBYCPBlueTint;
                float4 ifLocalVar44_g13187 = 0;
                if( temp_output_43_0_g13187 <= 0.6666667 )
                ifLocalVar44_g13187 = _RecolorRGBYCPCyanTint;
                else
                ifLocalVar44_g13187 = ifLocalVar46_g13187;
                float4 ifLocalVar47_g13187 = 0;
                if( temp_output_43_0_g13187 <= 0.3333333 )
                ifLocalVar47_g13187 = _RecolorRGBYCPYellowTint;
                else
                ifLocalVar47_g13187 = _RecolorRGBYCPGreenTint;
                float4 ifLocalVar45_g13187 = 0;
                if( temp_output_43_0_g13187 <= 0.1666667 )
                ifLocalVar45_g13187 = _RecolorRGBYCPRedTint;
                else
                ifLocalVar45_g13187 = ifLocalVar47_g13187;
                float4 ifLocalVar35_g13187 = 0;
                if( temp_output_43_0_g13187 >= 0.5 )
                ifLocalVar35_g13187 = ifLocalVar44_g13187;
                else
                ifLocalVar35_g13187 = ifLocalVar45_g13187;
                float4 break55_g13187 = ifLocalVar35_g13187;
                float3 appendResult56_g13187 = (float3(break55_g13187.r , break55_g13187.g , break55_g13187.b));
                float4 break2_g13188 = temp_output_1_0_g13187;
                float saferPower57_g13187 = abs( ( ( break2_g13188.x + break2_g13188.x + break2_g13188.y + break2_g13188.y + break2_g13188.y + break2_g13188.z ) / 6.0 ) );
                float3 lerpResult26_g13187 = lerp( (temp_output_1_0_g13187).rgb , ( appendResult56_g13187 * pow( saferPower57_g13187 , max( ( break55_g13187.a * 2.0 ) , 0.01 ) ) ) , ( hsvTorgb33_g13187.z * _RecolorRGBYCPFade ));
                float4 appendResult30_g13187 = (float4(lerpResult26_g13187 , temp_output_1_0_g13187.a));
                #ifdef _ENABLERECOLORRGBYCP_ON
                float4 staticSwitch9_g13157 = appendResult30_g13187;
                #else
                float4 staticSwitch9_g13157 = staticSwitch43_g13157;
                #endif
                float4 temp_output_1_0_g13161 = staticSwitch9_g13157;
                float3 temp_output_2_0_g13161 = (temp_output_1_0_g13161).rgb;
                float3 In115_g13161 = temp_output_2_0_g13161;
                float3 From115_g13161 = (_ColorReplaceFromColor).rgb;
                float4 break2_g13162 = temp_output_1_0_g13161;
                float3 To115_g13161 = ( pow( max( ( ( break2_g13162.x + break2_g13162.x + break2_g13162.y + break2_g13162.y + break2_g13162.y + break2_g13162.z ) / 6.0 ) , 0.0001 ) , max( _ColorReplaceContrast , 0.0001 ) ) * (_ColorReplaceToColor).rgb );
                float Fuzziness115_g13161 = _ColorReplaceSmoothness;
                float Range115_g13161 = _ColorReplaceRange;
                float3 localMyCustomExpression115_g13161 = MyCustomExpression115_g13161( In115_g13161 , From115_g13161 , To115_g13161 , Fuzziness115_g13161 , Range115_g13161 );
                float3 lerpResult112_g13161 = lerp( temp_output_2_0_g13161 , localMyCustomExpression115_g13161 , _ColorReplaceFade);
                float4 appendResult4_g13161 = (float4(lerpResult112_g13161 , temp_output_1_0_g13161.a));
                #ifdef _ENABLECOLORREPLACE_ON
                float4 staticSwitch29_g13157 = appendResult4_g13161;
                #else
                float4 staticSwitch29_g13157 = staticSwitch9_g13157;
                #endif
                float4 temp_output_1_0_g13174 = staticSwitch29_g13157;
                float3 temp_output_9_0_g13174 = (temp_output_1_0_g13174).rgb;
                float3 lerpResult3_g13174 = lerp( temp_output_9_0_g13174 , ( 1.0 - temp_output_9_0_g13174 ) , _NegativeFade);
                float4 appendResult8_g13174 = (float4(lerpResult3_g13174 , temp_output_1_0_g13174.a));
                #ifdef _ENABLENEGATIVE_ON
                float4 staticSwitch4_g13174 = appendResult8_g13174;
                #else
                float4 staticSwitch4_g13174 = temp_output_1_0_g13174;
                #endif
                float4 temp_output_57_0_g13157 = staticSwitch4_g13174;
                float4 temp_output_1_0_g13197 = temp_output_57_0_g13157;
                float3 temp_cast_34 = (max( _Contrast , 0.0001 )).xxx;
                float4 appendResult4_g13197 = (float4(pow( max( (temp_output_1_0_g13197).rgb , float3( 0.0001,0.0001,0.0001 ) ) , temp_cast_34 ) , temp_output_1_0_g13197.a));
                #ifdef _ENABLECONTRAST_ON
                float4 staticSwitch32_g13157 = appendResult4_g13197;
                #else
                float4 staticSwitch32_g13157 = temp_output_57_0_g13157;
                #endif
                float4 temp_output_2_0_g13172 = staticSwitch32_g13157;
                float4 appendResult6_g13172 = (float4(( (temp_output_2_0_g13172).rgb * _Brightness ) , temp_output_2_0_g13172.a));
                #ifdef _ENABLEBRIGHTNESS_ON
                float4 staticSwitch33_g13157 = appendResult6_g13172;
                #else
                float4 staticSwitch33_g13157 = staticSwitch32_g13157;
                #endif
                float4 temp_output_2_0_g13173 = staticSwitch33_g13157;
                float3 hsvTorgb1_g13173 = RGBToHSV( temp_output_2_0_g13173.rgb );
                float3 hsvTorgb3_g13173 = HSVToRGB( float3(( hsvTorgb1_g13173.x + _Hue ),hsvTorgb1_g13173.y,hsvTorgb1_g13173.z) );
                float4 appendResult8_g13173 = (float4(hsvTorgb3_g13173 , temp_output_2_0_g13173.a));
                #ifdef _ENABLEHUE_ON
                float4 staticSwitch36_g13157 = appendResult8_g13173;
                #else
                float4 staticSwitch36_g13157 = staticSwitch33_g13157;
                #endif
                float4 temp_output_1_0_g13191 = staticSwitch36_g13157;
                float4 break2_g13192 = temp_output_1_0_g13191;
                float temp_output_3_0_g13191 = ( ( break2_g13192.x + break2_g13192.x + break2_g13192.y + break2_g13192.y + break2_g13192.y + break2_g13192.z ) / 6.0 );
                float clampResult25_g13191 = clamp( ( ( ( ( temp_output_3_0_g13191 + _SplitToningShift ) - 0.5 ) * _SplitToningBalance ) + 0.5 ) , 0.0 , 1.0 );
                float3 lerpResult6_g13191 = lerp( (_SplitToningShadowsColor).rgb , (_SplitToningHighlightsColor).rgb , clampResult25_g13191);
                float3 lerpResult11_g13191 = lerp( (temp_output_1_0_g13191).rgb , ( lerpResult6_g13191 * pow( max( temp_output_3_0_g13191 , 0.0001 ) , max( _SplitToningContrast , 0.0001 ) ) ) , _SplitToningFade);
                float4 appendResult18_g13191 = (float4(lerpResult11_g13191 , temp_output_1_0_g13191.a));
                #ifdef _ENABLESPLITTONING_ON
                float4 staticSwitch30_g13157 = appendResult18_g13191;
                #else
                float4 staticSwitch30_g13157 = staticSwitch36_g13157;
                #endif
                float4 temp_output_1_0_g13169 = staticSwitch30_g13157;
                float3 temp_output_4_0_g13169 = (temp_output_1_0_g13169).rgb;
                float4 break12_g13169 = temp_output_1_0_g13169;
                float3 lerpResult7_g13169 = lerp( temp_output_4_0_g13169 , ( temp_output_4_0_g13169 + (_BlackTintColor).rgb ) , pow( max( ( 1.0 - min( max( max( break12_g13169.r , break12_g13169.g ) , break12_g13169.b ) , 1.0 ) ) , 0.0001 ) , max( _BlackTintPower , 0.0001 ) ));
                float3 lerpResult13_g13169 = lerp( temp_output_4_0_g13169 , lerpResult7_g13169 , _BlackTintFade);
                float4 appendResult11_g13169 = (float4(lerpResult13_g13169 , break12_g13169.a));
                #ifdef _ENABLEBLACKTINT_ON
                float4 staticSwitch20_g13157 = appendResult11_g13169;
                #else
                float4 staticSwitch20_g13157 = staticSwitch30_g13157;
                #endif
                float4 temp_output_1_0_g13183 = staticSwitch20_g13157;
                float4 break2_g13185 = temp_output_1_0_g13183;
                float2 temp_output_65_0_g13183 = shaderPosition235;
                float linValue16_g13184 = tex2D( _UberNoiseTexture, ( temp_output_65_0_g13183 * _InkSpreadNoiseScale ) ).r;
                float localMyCustomExpression16_g13184 = MyCustomExpression16_g13184( linValue16_g13184 );
                float clampResult53_g13183 = clamp( ( ( ( _InkSpreadDistance - distance( _InkSpreadPosition , temp_output_65_0_g13183 ) ) + ( localMyCustomExpression16_g13184 * _InkSpreadNoiseFactor ) ) / max( _InkSpreadWidth , 0.001 ) ) , 0.0 , 1.0 );
                float3 lerpResult7_g13183 = lerp( (temp_output_1_0_g13183).rgb , ( (_InkSpreadColor).rgb * pow( max( ( ( break2_g13185.x + break2_g13185.x + break2_g13185.y + break2_g13185.y + break2_g13185.y + break2_g13185.z ) / 6.0 ) , 0.0001 ) , max( _InkSpreadContrast , 0.0001 ) ) ) , ( _InkSpreadFade * clampResult53_g13183 ));
                float4 appendResult9_g13183 = (float4(lerpResult7_g13183 , (temp_output_1_0_g13183).a));
                #ifdef _ENABLEINKSPREAD_ON
                float4 staticSwitch17_g13157 = appendResult9_g13183;
                #else
                float4 staticSwitch17_g13157 = staticSwitch20_g13157;
                #endif
                float temp_output_39_0_g13157 = shaderTime237;
                float4 temp_output_1_0_g13175 = staticSwitch17_g13157;
                float3 hsvTorgb15_g13175 = RGBToHSV( (temp_output_1_0_g13175).rgb );
                float3 hsvTorgb19_g13175 = HSVToRGB( float3(( ( temp_output_39_0_g13157 * _ShiftHueSpeed ) + hsvTorgb15_g13175.x ),hsvTorgb15_g13175.y,hsvTorgb15_g13175.z) );
                float4 appendResult6_g13175 = (float4(hsvTorgb19_g13175 , temp_output_1_0_g13175.a));
                #ifdef _ENABLESHIFTHUE_ON
                float4 staticSwitch19_g13157 = appendResult6_g13175;
                #else
                float4 staticSwitch19_g13157 = staticSwitch17_g13157;
                #endif
                float3 hsvTorgb19_g13179 = HSVToRGB( float3(( ( temp_output_39_0_g13157 * _AddHueSpeed ) % 1.0 ),_AddHueSaturation,_AddHueBrightness) );
                float4 temp_output_1_0_g13179 = staticSwitch19_g13157;
                float4 break2_g13181 = temp_output_1_0_g13179;
                float2 uv_AddHueMask = IN.texcoord.xy * _AddHueMask_ST.xy + _AddHueMask_ST.zw;
                float4 tex2DNode3_g13180 = tex2D( _AddHueMask, uv_AddHueMask );
                #ifdef _ADDHUEMASKTOGGLE_ON
                float staticSwitch33_g13179 = ( _AddHueFade * ( tex2DNode3_g13180.r * tex2DNode3_g13180.a ) );
                #else
                float staticSwitch33_g13179 = _AddHueFade;
                #endif
                float4 appendResult6_g13179 = (float4(( ( hsvTorgb19_g13179 * pow( max( ( ( break2_g13181.x + break2_g13181.x + break2_g13181.y + break2_g13181.y + break2_g13181.y + break2_g13181.z ) / 6.0 ) , 0.0001 ) , max( _AddHueContrast , 0.0001 ) ) * staticSwitch33_g13179 ) + (temp_output_1_0_g13179).rgb ) , temp_output_1_0_g13179.a));
                #ifdef _ENABLEADDHUE_ON
                float4 staticSwitch23_g13157 = appendResult6_g13179;
                #else
                float4 staticSwitch23_g13157 = staticSwitch19_g13157;
                #endif
                float4 temp_output_1_0_g13176 = staticSwitch23_g13157;
                float4 break2_g13177 = temp_output_1_0_g13176;
                float3 temp_output_13_0_g13176 = (_SineGlowColor).rgb;
                float2 uv_SineGlowMask = IN.texcoord.xy * _SineGlowMask_ST.xy + _SineGlowMask_ST.zw;
                float4 tex2DNode30_g13176 = tex2D( _SineGlowMask, uv_SineGlowMask );
                #ifdef _SINEGLOWMASKTOGGLE_ON
                float3 staticSwitch27_g13176 = ( (tex2DNode30_g13176).rgb * temp_output_13_0_g13176 * tex2DNode30_g13176.a );
                #else
                float3 staticSwitch27_g13176 = temp_output_13_0_g13176;
                #endif
                float4 appendResult21_g13176 = (float4(( (temp_output_1_0_g13176).rgb + ( pow( max( ( ( break2_g13177.x + break2_g13177.x + break2_g13177.y + break2_g13177.y + break2_g13177.y + break2_g13177.z ) / 6.0 ) , 0.0001 ) , max( _SineGlowContrast , 0.0001 ) ) * staticSwitch27_g13176 * _SineGlowFade * ( ( ( sin( ( temp_output_39_0_g13157 * _SineGlowFrequency ) ) + 1.0 ) * ( _SineGlowMax - _SineGlowMin ) ) + _SineGlowMin ) ) ) , temp_output_1_0_g13176.a));
                #ifdef _ENABLESINEGLOW_ON
                float4 staticSwitch28_g13157 = appendResult21_g13176;
                #else
                float4 staticSwitch28_g13157 = staticSwitch23_g13157;
                #endif
                float4 temp_output_1_0_g13164 = staticSwitch28_g13157;
                float4 break2_g13165 = temp_output_1_0_g13164;
                float3 temp_cast_50 = (( ( break2_g13165.x + break2_g13165.x + break2_g13165.y + break2_g13165.y + break2_g13165.y + break2_g13165.z ) / 6.0 )).xxx;
                float3 lerpResult5_g13164 = lerp( temp_cast_50 , (temp_output_1_0_g13164).rgb , _Saturation);
                float4 appendResult8_g13164 = (float4(lerpResult5_g13164 , temp_output_1_0_g13164.a));
                #ifdef _ENABLESATURATION_ON
                float4 staticSwitch38_g13157 = appendResult8_g13164;
                #else
                float4 staticSwitch38_g13157 = staticSwitch28_g13157;
                #endif
                float4 temp_output_15_0_g13166 = staticSwitch38_g13157;
                float3 temp_output_82_0_g13166 = (_InnerOutlineColor).rgb;
                float2 temp_output_7_0_g13166 = temp_output_1_0_g13157;
                float temp_output_179_0_g13166 = temp_output_39_0_g13157;
                #ifdef _INNEROUTLINETEXTURETOGGLE_ON
                float3 staticSwitch187_g13166 = ( (tex2D( _InnerOutlineTintTexture, ( temp_output_7_0_g13166 + ( _InnerOutlineTextureSpeed * temp_output_179_0_g13166 ) ) )).rgb * temp_output_82_0_g13166 );
                #else
                float3 staticSwitch187_g13166 = temp_output_82_0_g13166;
                #endif
                float linValue16_g13168 = tex2D( _UberNoiseTexture, ( ( ( temp_output_179_0_g13166 * _InnerOutlineNoiseSpeed ) + temp_output_7_0_g13166 ) * _InnerOutlineNoiseScale ) ).r;
                float localMyCustomExpression16_g13168 = MyCustomExpression16_g13168( linValue16_g13168 );
                #ifdef _INNEROUTLINEDISTORTIONTOGGLE_ON
                float2 staticSwitch169_g13166 = ( ( localMyCustomExpression16_g13168 - 0.5 ) * _InnerOutlineDistortionIntensity );
                #else
                float2 staticSwitch169_g13166 = float2( 0,0 );
                #endif
                float2 temp_output_131_0_g13166 = ( staticSwitch169_g13166 + temp_output_7_0_g13166 );
                float2 appendResult10_g13167 = (float2(_MainTex_TexelSize.z , _MainTex_TexelSize.w));
                float2 appendResult2_g13167 = (float2(1.0 , ( _MainTex_TexelSize.z / _MainTex_TexelSize.w )));
                #ifdef _RESIZABLETEXTURE_ON
                float2 staticSwitch9_g13167 = appendResult2_g13167;
                #else
                float2 staticSwitch9_g13167 = ( float2( 100,100 ) / appendResult10_g13167 );
                #endif
                float2 temp_output_25_0_g13166 = staticSwitch9_g13167;
                float temp_output_178_0_g13166 = ( _InnerOutlineFade * ( 1.0 - min( min( min( min( min( min( min( tex2D( _MainTex, ( temp_output_131_0_g13166 + ( ( _InnerOutlineWidth * float2( 0,-1 ) ) * temp_output_25_0_g13166 ) ) ).a , tex2D( _MainTex, ( temp_output_131_0_g13166 + ( ( _InnerOutlineWidth * float2( 0,1 ) ) * temp_output_25_0_g13166 ) ) ).a ) , tex2D( _MainTex, ( temp_output_131_0_g13166 + ( ( _InnerOutlineWidth * float2( -1,0 ) ) * temp_output_25_0_g13166 ) ) ).a ) , tex2D( _MainTex, ( temp_output_131_0_g13166 + ( ( _InnerOutlineWidth * float2( 1,0 ) ) * temp_output_25_0_g13166 ) ) ).a ) , tex2D( _MainTex, ( temp_output_131_0_g13166 + ( ( _InnerOutlineWidth * float2( 0.705,0.705 ) ) * temp_output_25_0_g13166 ) ) ).a ) , tex2D( _MainTex, ( temp_output_131_0_g13166 + ( ( _InnerOutlineWidth * float2( -0.705,0.705 ) ) * temp_output_25_0_g13166 ) ) ).a ) , tex2D( _MainTex, ( temp_output_131_0_g13166 + ( ( _InnerOutlineWidth * float2( 0.705,-0.705 ) ) * temp_output_25_0_g13166 ) ) ).a ) , tex2D( _MainTex, ( temp_output_131_0_g13166 + ( ( _InnerOutlineWidth * float2( -0.705,-0.705 ) ) * temp_output_25_0_g13166 ) ) ).a ) ) );
                float3 lerpResult176_g13166 = lerp( (temp_output_15_0_g13166).rgb , staticSwitch187_g13166 , temp_output_178_0_g13166);
                #ifdef _INNEROUTLINEOUTLINEONLYTOGGLE_ON
                float staticSwitch188_g13166 = ( temp_output_178_0_g13166 * temp_output_15_0_g13166.a );
                #else
                float staticSwitch188_g13166 = temp_output_15_0_g13166.a;
                #endif
                float4 appendResult177_g13166 = (float4(lerpResult176_g13166 , staticSwitch188_g13166));
                #ifdef _ENABLEINNEROUTLINE_ON
                float4 staticSwitch12_g13157 = appendResult177_g13166;
                #else
                float4 staticSwitch12_g13157 = staticSwitch38_g13157;
                #endif
                float4 temp_output_15_0_g13194 = staticSwitch12_g13157;
                float3 temp_output_82_0_g13194 = (_OuterOutlineColor).rgb;
                float2 temp_output_7_0_g13194 = temp_output_1_0_g13157;
                float temp_output_186_0_g13194 = temp_output_39_0_g13157;
                #ifdef _OUTEROUTLINETEXTURETOGGLE_ON
                float3 staticSwitch199_g13194 = ( (tex2D( _OuterOutlineTintTexture, ( temp_output_7_0_g13194 + ( _OuterOutlineTextureSpeed * temp_output_186_0_g13194 ) ) )).rgb * temp_output_82_0_g13194 );
                #else
                float3 staticSwitch199_g13194 = temp_output_82_0_g13194;
                #endif
                float temp_output_182_0_g13194 = ( ( 1.0 - temp_output_15_0_g13194.a ) * min( ( _OuterOutlineFade * 3.0 ) , 1.0 ) );
                #ifdef _OUTEROUTLINEOUTLINEONLYTOGGLE_ON
                float staticSwitch203_g13194 = 1.0;
                #else
                float staticSwitch203_g13194 = temp_output_182_0_g13194;
                #endif
                float3 lerpResult178_g13194 = lerp( (temp_output_15_0_g13194).rgb , staticSwitch199_g13194 , staticSwitch203_g13194);
                float3 lerpResult170_g13194 = lerp( lerpResult178_g13194 , staticSwitch199_g13194 , staticSwitch203_g13194);
                float linValue16_g13195 = tex2D( _UberNoiseTexture, ( ( ( temp_output_186_0_g13194 * _OuterOutlineNoiseSpeed ) + temp_output_7_0_g13194 ) * _OuterOutlineNoiseScale ) ).r;
                float localMyCustomExpression16_g13195 = MyCustomExpression16_g13195( linValue16_g13195 );
                #ifdef _OUTEROUTLINEDISTORTIONTOGGLE_ON
                float2 staticSwitch157_g13194 = ( ( localMyCustomExpression16_g13195 - 0.5 ) * _OuterOutlineDistortionIntensity );
                #else
                float2 staticSwitch157_g13194 = float2( 0,0 );
                #endif
                float2 temp_output_131_0_g13194 = ( staticSwitch157_g13194 + temp_output_7_0_g13194 );
                float2 appendResult10_g13196 = (float2(_MainTex_TexelSize.z , _MainTex_TexelSize.w));
                float2 appendResult2_g13196 = (float2(1.0 , ( _MainTex_TexelSize.z / _MainTex_TexelSize.w )));
                #ifdef _RESIZABLETEXTURE_ON
                float2 staticSwitch9_g13196 = appendResult2_g13196;
                #else
                float2 staticSwitch9_g13196 = ( float2( 100,100 ) / appendResult10_g13196 );
                #endif
                float2 temp_output_25_0_g13194 = staticSwitch9_g13196;
                float lerpResult168_g13194 = lerp( temp_output_15_0_g13194.a , min( ( max( max( max( max( max( max( max( tex2D( _MainTex, ( temp_output_131_0_g13194 + ( ( _OuterOutlineWidth * float2( 0,-1 ) ) * temp_output_25_0_g13194 ) ) ).a , tex2D( _MainTex, ( temp_output_131_0_g13194 + ( ( _OuterOutlineWidth * float2( 0,1 ) ) * temp_output_25_0_g13194 ) ) ).a ) , tex2D( _MainTex, ( temp_output_131_0_g13194 + ( ( _OuterOutlineWidth * float2( -1,0 ) ) * temp_output_25_0_g13194 ) ) ).a ) , tex2D( _MainTex, ( temp_output_131_0_g13194 + ( ( _OuterOutlineWidth * float2( 1,0 ) ) * temp_output_25_0_g13194 ) ) ).a ) , tex2D( _MainTex, ( temp_output_131_0_g13194 + ( ( _OuterOutlineWidth * float2( 0.705,0.705 ) ) * temp_output_25_0_g13194 ) ) ).a ) , tex2D( _MainTex, ( temp_output_131_0_g13194 + ( ( _OuterOutlineWidth * float2( -0.705,0.705 ) ) * temp_output_25_0_g13194 ) ) ).a ) , tex2D( _MainTex, ( temp_output_131_0_g13194 + ( ( _OuterOutlineWidth * float2( 0.705,-0.705 ) ) * temp_output_25_0_g13194 ) ) ).a ) , tex2D( _MainTex, ( temp_output_131_0_g13194 + ( ( _OuterOutlineWidth * float2( -0.705,-0.705 ) ) * temp_output_25_0_g13194 ) ) ).a ) * 3.0 ) , 1.0 ) , _OuterOutlineFade);
                #ifdef _OUTEROUTLINEOUTLINEONLYTOGGLE_ON
                float staticSwitch200_g13194 = ( temp_output_182_0_g13194 * lerpResult168_g13194 );
                #else
                float staticSwitch200_g13194 = lerpResult168_g13194;
                #endif
                float4 appendResult174_g13194 = (float4(lerpResult170_g13194 , staticSwitch200_g13194));
                #ifdef _ENABLEOUTEROUTLINE_ON
                float4 staticSwitch13_g13157 = appendResult174_g13194;
                #else
                float4 staticSwitch13_g13157 = staticSwitch12_g13157;
                #endif
                float4 temp_output_15_0_g13171 = staticSwitch13_g13157;
                float3 temp_output_82_0_g13171 = (_PixelOutlineColor).rgb;
                float2 temp_output_7_0_g13171 = temp_output_1_0_g13157;
                #ifdef _PIXELOUTLINETEXTURETOGGLE_ON
                float3 staticSwitch199_g13171 = ( (tex2D( _PixelOutlineTintTexture, ( temp_output_7_0_g13171 + ( _PixelOutlineTextureSpeed * temp_output_39_0_g13157 ) ) )).rgb * temp_output_82_0_g13171 );
                #else
                float3 staticSwitch199_g13171 = temp_output_82_0_g13171;
                #endif
                float temp_output_213_0_g13171 = ( _PixelOutlineFade * step( temp_output_15_0_g13171.a , _PixelOutlineAlphaLimit ) );
                float temp_output_182_0_g13171 = ( ( 1.0 - temp_output_15_0_g13171.a ) * min( ( temp_output_213_0_g13171 * 3.0 ) , 1.0 ) );
                #ifdef _PIXELOUTLINEOUTLINEONLYTOGGLE_ON
                float staticSwitch203_g13171 = 1.0;
                #else
                float staticSwitch203_g13171 = temp_output_182_0_g13171;
                #endif
                float3 lerpResult178_g13171 = lerp( (temp_output_15_0_g13171).rgb , staticSwitch199_g13171 , staticSwitch203_g13171);
                float3 lerpResult170_g13171 = lerp( lerpResult178_g13171 , staticSwitch199_g13171 , staticSwitch203_g13171);
                float2 appendResult206_g13171 = (float2(_MainTex_TexelSize.z , _MainTex_TexelSize.w));
                float2 temp_output_209_0_g13171 = ( float2( 1,1 ) / appendResult206_g13171 );
                float lerpResult168_g13171 = lerp( temp_output_15_0_g13171.a , min( ( max( max( max( tex2D( _MainTex, ( temp_output_7_0_g13171 + ( ( _PixelOutlineWidth * float2( 0,-1 ) ) * temp_output_209_0_g13171 ) ) ).a , tex2D( _MainTex, ( temp_output_7_0_g13171 + ( ( _PixelOutlineWidth * float2( 0,1 ) ) * temp_output_209_0_g13171 ) ) ).a ) , tex2D( _MainTex, ( temp_output_7_0_g13171 + ( ( _PixelOutlineWidth * float2( -1,0 ) ) * temp_output_209_0_g13171 ) ) ).a ) , tex2D( _MainTex, ( temp_output_7_0_g13171 + ( ( _PixelOutlineWidth * float2( 1,0 ) ) * temp_output_209_0_g13171 ) ) ).a ) * 3.0 ) , 1.0 ) , temp_output_213_0_g13171);
                #ifdef _PIXELOUTLINEOUTLINEONLYTOGGLE_ON
                float staticSwitch200_g13171 = ( temp_output_182_0_g13171 * lerpResult168_g13171 );
                #else
                float staticSwitch200_g13171 = lerpResult168_g13171;
                #endif
                float4 appendResult174_g13171 = (float4(lerpResult170_g13171 , staticSwitch200_g13171));
                #ifdef _ENABLEPIXELOUTLINE_ON
                float4 staticSwitch48_g13157 = appendResult174_g13171;
                #else
                float4 staticSwitch48_g13157 = staticSwitch13_g13157;
                #endif
                float3 lerpResult15_g13158 = lerp( (_PingPongGlowFrom).rgb , (_PingPongGlowTo).rgb , ( ( sin( ( temp_output_39_0_g13157 * _PingPongGlowFrequency ) ) + 1.0 ) / 2.0 ));
                float4 temp_output_5_0_g13158 = staticSwitch48_g13157;
                float4 break2_g13159 = temp_output_5_0_g13158;
                float4 appendResult12_g13158 = (float4(( ( lerpResult15_g13158 * _PingPongGlowFade * pow( max( ( ( break2_g13159.x + break2_g13159.x + break2_g13159.y + break2_g13159.y + break2_g13159.y + break2_g13159.z ) / 6.0 ) , 0.0001 ) , max( _PingPongGlowContrast , 0.0001 ) ) ) + (temp_output_5_0_g13158).rgb ) , temp_output_5_0_g13158.a));
                #ifdef _ENABLEPINGPONGGLOW_ON
                float4 staticSwitch46_g13157 = appendResult12_g13158;
                #else
                float4 staticSwitch46_g13157 = staticSwitch48_g13157;
                #endif
                float4 temp_output_361_0 = staticSwitch46_g13157;
                float4 temp_output_1_0_g13199 = temp_output_361_0;
                float4 break2_g13200 = temp_output_1_0_g13199;
                float temp_output_44_0_g13199 = 0.5;
                float4 appendResult22_g13199 = (float4(( (_HologramTint).rgb * pow( max( ( ( break2_g13200.x + break2_g13200.x + break2_g13200.y + break2_g13200.y + break2_g13200.y + break2_g13200.z ) / 6.0 ) , 0.0001 ) , max( _HologramContrast , 0.0001 ) ) ) , ( max( pow( abs( sin( ( ( ( ( shaderTime237 * ( _HologramLineSpeed * temp_output_44_0_g13199 * 0.2 ) ) + ase_positionSSNorm.y ) / temp_output_44_0_g13199 ) * _HologramLineFrequency ) ) ) , _HologramLineGap ) , _HologramMinAlpha ) * temp_output_1_0_g13199.a )));
                float4 lerpResult37_g13199 = lerp( temp_output_1_0_g13199 , appendResult22_g13199 , hologramFade182);
                #ifdef _ENABLEHOLOGRAM_ON
                float4 staticSwitch56 = lerpResult37_g13199;
                #else
                float4 staticSwitch56 = temp_output_361_0;
                #endif
                float4 temp_output_1_0_g13202 = staticSwitch56;
                float4 break2_g13204 = temp_output_1_0_g13202;
                float temp_output_34_0_g13202 = shaderTime237;
                float linValue16_g13203 = tex2D( _UberNoiseTexture, ( ( glitchPosition154 + ( _GlitchNoiseSpeed * temp_output_34_0_g13202 ) ) * _GlitchNoiseScale ) ).r;
                float localMyCustomExpression16_g13203 = MyCustomExpression16_g13203( linValue16_g13203 );
                float3 hsvTorgb3_g13205 = HSVToRGB( float3(( localMyCustomExpression16_g13203 + ( temp_output_34_0_g13202 * _GlitchHueSpeed ) ),1.0,1.0) );
                float3 lerpResult23_g13202 = lerp( (temp_output_1_0_g13202).rgb , ( ( ( break2_g13204.x + break2_g13204.x + break2_g13204.y + break2_g13204.y + break2_g13204.y + break2_g13204.z ) / 6.0 ) * _GlitchBrightness * hsvTorgb3_g13205 ) , glitchFade152);
                float4 appendResult27_g13202 = (float4(lerpResult23_g13202 , temp_output_1_0_g13202.a));
                #ifdef _ENABLEGLITCH_ON
                float4 staticSwitch57 = appendResult27_g13202;
                #else
                float4 staticSwitch57 = staticSwitch56;
                #endif
                float4 temp_output_3_0_g13207 = staticSwitch57;
                float4 temp_output_1_0_g13237 = temp_output_3_0_g13207;
                float2 temp_output_41_0_g13207 = shaderPosition235;
                float2 temp_output_99_0_g13237 = temp_output_41_0_g13207;
                float temp_output_40_0_g13207 = shaderTime237;
                float linValue16_g13242 = tex2D( _UberNoiseTexture, ( ( ( temp_output_40_0_g13207 * _CamouflageDistortionSpeed ) + temp_output_99_0_g13237 ) * _CamouflageDistortionScale ) ).r;
                float localMyCustomExpression16_g13242 = MyCustomExpression16_g13242( linValue16_g13242 );
                #ifdef _CAMOUFLAGEANIMATIONTOGGLE_ON
                float2 staticSwitch101_g13237 = ( ( ( localMyCustomExpression16_g13242 - 0.25 ) * _CamouflageDistortionIntensity ) + temp_output_99_0_g13237 );
                #else
                float2 staticSwitch101_g13237 = temp_output_99_0_g13237;
                #endif
                float linValue16_g13239 = tex2D( _UberNoiseTexture, ( staticSwitch101_g13237 * _CamouflageNoiseScaleA ) ).r;
                float localMyCustomExpression16_g13239 = MyCustomExpression16_g13239( linValue16_g13239 );
                float clampResult52_g13237 = clamp( ( ( _CamouflageDensityA - localMyCustomExpression16_g13239 ) / max( _CamouflageSmoothnessA , 0.005 ) ) , 0.0 , 1.0 );
                float4 lerpResult55_g13237 = lerp( _CamouflageBaseColor , ( _CamouflageColorA * clampResult52_g13237 ) , clampResult52_g13237);
                float linValue16_g13241 = tex2D( _UberNoiseTexture, ( ( staticSwitch101_g13237 + float2( 12.3,12.3 ) ) * _CamouflageNoiseScaleB ) ).r;
                float localMyCustomExpression16_g13241 = MyCustomExpression16_g13241( linValue16_g13241 );
                float clampResult65_g13237 = clamp( ( ( _CamouflageDensityB - localMyCustomExpression16_g13241 ) / max( _CamouflageSmoothnessB , 0.005 ) ) , 0.0 , 1.0 );
                float4 lerpResult68_g13237 = lerp( lerpResult55_g13237 , ( _CamouflageColorB * clampResult65_g13237 ) , clampResult65_g13237);
                float4 break2_g13240 = temp_output_1_0_g13237;
                float3 lerpResult4_g13237 = lerp( (temp_output_1_0_g13237).rgb , ( (lerpResult68_g13237).rgb * pow( max( ( ( break2_g13240.x + break2_g13240.x + break2_g13240.y + break2_g13240.y + break2_g13240.y + break2_g13240.z ) / 6.0 ) , 0.0001 ) , max( _CamouflageContrast , 0.0001 ) ) ) , _CamouflageFade);
                float4 appendResult7_g13237 = (float4(lerpResult4_g13237 , temp_output_1_0_g13237.a));
                #ifdef _ENABLECAMOUFLAGE_ON
                float4 staticSwitch26_g13207 = appendResult7_g13237;
                #else
                float4 staticSwitch26_g13207 = temp_output_3_0_g13207;
                #endif
                float4 temp_output_1_0_g13230 = staticSwitch26_g13207;
                float temp_output_59_0_g13230 = temp_output_40_0_g13207;
                float2 temp_output_58_0_g13230 = temp_output_41_0_g13207;
                float linValue16_g13231 = tex2D( _UberNoiseTexture, ( ( ( temp_output_59_0_g13230 * _MetalNoiseDistortionSpeed ) + temp_output_58_0_g13230 ) * _MetalNoiseDistortionScale ) ).r;
                float localMyCustomExpression16_g13231 = MyCustomExpression16_g13231( linValue16_g13231 );
                float linValue16_g13233 = tex2D( _UberNoiseTexture, ( ( ( ( localMyCustomExpression16_g13231 - 0.25 ) * _MetalNoiseDistortion ) + ( ( temp_output_59_0_g13230 * _MetalNoiseSpeed ) + temp_output_58_0_g13230 ) ) * _MetalNoiseScale ) ).r;
                float localMyCustomExpression16_g13233 = MyCustomExpression16_g13233( linValue16_g13233 );
                float4 break2_g13232 = temp_output_1_0_g13230;
                float temp_output_5_0_g13230 = ( ( break2_g13232.x + break2_g13232.x + break2_g13232.y + break2_g13232.y + break2_g13232.y + break2_g13232.z ) / 6.0 );
                float2 uv_MetalMask = IN.texcoord.xy * _MetalMask_ST.xy + _MetalMask_ST.zw;
                float4 tex2DNode3_g13235 = tex2D( _MetalMask, uv_MetalMask );
                #ifdef _METALMASKTOGGLE_ON
                float staticSwitch60_g13230 = ( _MetalFade * ( tex2DNode3_g13235.r * tex2DNode3_g13235.a ) );
                #else
                float staticSwitch60_g13230 = _MetalFade;
                #endif
                float4 lerpResult45_g13230 = lerp( temp_output_1_0_g13230 , ( ( max( ( ( _MetalHighlightDensity - localMyCustomExpression16_g13233 ) / max( _MetalHighlightDensity , 0.01 ) ) , 0.0 ) * _MetalHighlightColor * pow( max( temp_output_5_0_g13230 , 0.0001 ) , max( _MetalHighlightContrast , 0.0001 ) ) ) + ( pow( max( temp_output_5_0_g13230 , 0.0001 ) , max( _MetalContrast , 0.0001 ) ) * _MetalColor ) ) , staticSwitch60_g13230);
                float4 appendResult8_g13230 = (float4((lerpResult45_g13230).rgb , (temp_output_1_0_g13230).a));
                #ifdef _ENABLEMETAL_ON
                float4 staticSwitch28_g13207 = appendResult8_g13230;
                #else
                float4 staticSwitch28_g13207 = staticSwitch26_g13207;
                #endif
                float4 temp_output_1_0_g13222 = staticSwitch28_g13207;
                float4 break2_g13223 = temp_output_1_0_g13222;
                float temp_output_7_0_g13222 = ( ( break2_g13223.x + break2_g13223.x + break2_g13223.y + break2_g13223.y + break2_g13223.y + break2_g13223.z ) / 6.0 );
                float2 temp_output_72_0_g13222 = temp_output_41_0_g13207;
                float linValue16_g13224 = tex2D( _UberNoiseTexture, ( temp_output_72_0_g13222 * _FrozenSnowScale ) ).r;
                float localMyCustomExpression16_g13224 = MyCustomExpression16_g13224( linValue16_g13224 );
                float temp_output_73_0_g13222 = temp_output_40_0_g13207;
                float linValue16_g13226 = tex2D( _UberNoiseTexture, ( ( ( temp_output_73_0_g13222 * _FrozenHighlightDistortionSpeed ) + temp_output_72_0_g13222 ) * _FrozenHighlightDistortionScale ) ).r;
                float localMyCustomExpression16_g13226 = MyCustomExpression16_g13226( linValue16_g13226 );
                float linValue16_g13227 = tex2D( _UberNoiseTexture, ( ( ( ( localMyCustomExpression16_g13226 - 0.25 ) * _FrozenHighlightDistortion ) + ( ( temp_output_73_0_g13222 * _FrozenHighlightSpeed ) + temp_output_72_0_g13222 ) ) * _FrozenHighlightScale ) ).r;
                float localMyCustomExpression16_g13227 = MyCustomExpression16_g13227( linValue16_g13227 );
                float3 lerpResult57_g13222 = lerp( (temp_output_1_0_g13222).rgb , ( ( pow( max( temp_output_7_0_g13222 , 0.0001 ) , max( _FrozenContrast , 0.0001 ) ) * (_FrozenTint).rgb ) + ( pow( max( temp_output_7_0_g13222 , 0.0001 ) , max( _FrozenSnowContrast , 0.0001 ) ) * ( (_FrozenSnowColor).rgb * max( ( _FrozenSnowDensity - localMyCustomExpression16_g13224 ) , 0.0 ) ) ) + (( max( ( ( _FrozenHighlightDensity - localMyCustomExpression16_g13227 ) / max( _FrozenHighlightDensity , 0.01 ) ) , 0.0 ) * _FrozenHighlightColor * pow( max( temp_output_7_0_g13222 , 0.0001 ) , max( _FrozenHighlightContrast , 0.0001 ) ) )).rgb ) , _FrozenFade);
                float4 appendResult26_g13222 = (float4(lerpResult57_g13222 , temp_output_1_0_g13222.a));
                #ifdef _ENABLEFROZEN_ON
                float4 staticSwitch29_g13207 = appendResult26_g13222;
                #else
                float4 staticSwitch29_g13207 = staticSwitch28_g13207;
                #endif
                float4 temp_output_1_0_g13216 = staticSwitch29_g13207;
                float3 temp_output_28_0_g13216 = (temp_output_1_0_g13216).rgb;
                float4 break2_g13220 = float4( temp_output_28_0_g13216 , 0.0 );
                float2 temp_output_72_0_g13216 = temp_output_41_0_g13207;
                float linValue16_g13219 = tex2D( _UberNoiseTexture, ( temp_output_72_0_g13216 * _BurnSwirlNoiseScale ) ).r;
                float localMyCustomExpression16_g13219 = MyCustomExpression16_g13219( linValue16_g13219 );
                float linValue16_g13217 = tex2D( _UberNoiseTexture, ( ( ( ( localMyCustomExpression16_g13219 - 0.5 ) * float2( 1,1 ) * _BurnSwirlFactor ) + temp_output_72_0_g13216 ) * _BurnInsideNoiseScale ) ).r;
                float localMyCustomExpression16_g13217 = MyCustomExpression16_g13217( linValue16_g13217 );
                float clampResult68_g13216 = clamp( ( _BurnInsideNoiseFactor - localMyCustomExpression16_g13217 ) , 0.0 , 1.0 );
                float linValue16_g13218 = tex2D( _UberNoiseTexture, ( temp_output_72_0_g13216 * _BurnEdgeNoiseScale ) ).r;
                float localMyCustomExpression16_g13218 = MyCustomExpression16_g13218( linValue16_g13218 );
                float temp_output_15_0_g13216 = ( ( ( _BurnRadius - distance( temp_output_72_0_g13216 , _BurnPosition ) ) + ( localMyCustomExpression16_g13218 * _BurnEdgeNoiseFactor ) ) / max( _BurnWidth , 0.01 ) );
                float clampResult18_g13216 = clamp( temp_output_15_0_g13216 , 0.0 , 1.0 );
                float3 lerpResult29_g13216 = lerp( temp_output_28_0_g13216 , ( pow( max( ( ( break2_g13220.x + break2_g13220.x + break2_g13220.y + break2_g13220.y + break2_g13220.y + break2_g13220.z ) / 6.0 ) , 0.0001 ) , max( _BurnInsideContrast , 0.0001 ) ) * ( ( (_BurnInsideNoiseColor).rgb * clampResult68_g13216 ) + (_BurnInsideColor).rgb ) ) , clampResult18_g13216);
                float3 lerpResult40_g13216 = lerp( temp_output_28_0_g13216 , ( lerpResult29_g13216 + ( ( step( temp_output_15_0_g13216 , 1.0 ) * step( 0.0 , temp_output_15_0_g13216 ) ) * (_BurnEdgeColor).rgb ) ) , _BurnFade);
                float4 appendResult43_g13216 = (float4(lerpResult40_g13216 , temp_output_1_0_g13216.a));
                #ifdef _ENABLEBURN_ON
                float4 staticSwitch32_g13207 = appendResult43_g13216;
                #else
                float4 staticSwitch32_g13207 = staticSwitch29_g13207;
                #endif
                float2 temp_output_42_0_g13212 = temp_output_41_0_g13207;
                float linValue16_g13213 = tex2D( _UberNoiseTexture, ( temp_output_42_0_g13212 * _RainbowNoiseScale ) ).r;
                float localMyCustomExpression16_g13213 = MyCustomExpression16_g13213( linValue16_g13213 );
                float3 hsvTorgb3_g13215 = HSVToRGB( float3(( ( ( distance( temp_output_42_0_g13212 , _RainbowCenter ) + ( localMyCustomExpression16_g13213 * _RainbowNoiseFactor ) ) * _RainbowDensity ) + ( _RainbowSpeed * temp_output_40_0_g13207 ) ),1.0,1.0) );
                float3 hsvTorgb36_g13212 = RGBToHSV( hsvTorgb3_g13215 );
                float3 hsvTorgb37_g13212 = HSVToRGB( float3(hsvTorgb36_g13212.x,_RainbowSaturation,( hsvTorgb36_g13212.z * _RainbowBrightness )) );
                float4 temp_output_1_0_g13212 = staticSwitch32_g13207;
                float4 break2_g13214 = temp_output_1_0_g13212;
                float saferPower24_g13212 = abs( ( ( break2_g13214.x + break2_g13214.x + break2_g13214.y + break2_g13214.y + break2_g13214.y + break2_g13214.z ) / 6.0 ) );
                float4 appendResult29_g13212 = (float4(( ( hsvTorgb37_g13212 * pow( saferPower24_g13212 , max( _RainbowContrast , 0.001 ) ) * _RainbowFade ) + (temp_output_1_0_g13212).rgb ) , temp_output_1_0_g13212.a));
                #ifdef _ENABLERAINBOW_ON
                float4 staticSwitch34_g13207 = appendResult29_g13212;
                #else
                float4 staticSwitch34_g13207 = staticSwitch32_g13207;
                #endif
                float4 temp_output_1_0_g13208 = staticSwitch34_g13207;
                float3 temp_output_57_0_g13208 = (temp_output_1_0_g13208).rgb;
                float4 break2_g13210 = temp_output_1_0_g13208;
                float3 temp_cast_73 = (( ( break2_g13210.x + break2_g13210.x + break2_g13210.y + break2_g13210.y + break2_g13210.y + break2_g13210.z ) / 6.0 )).xxx;
                float3 lerpResult92_g13208 = lerp( temp_cast_73 , temp_output_57_0_g13208 , _ShineSaturation);
                float3 temp_cast_74 = (max( max( _ShineContrast , 0.001 ) , 0.0001 )).xxx;
                float3 rotatedValue69_g13208 = RotateAroundAxis( float3( 0,0,0 ), float3( ( _ShineFrequency * temp_output_41_0_g13207 ) ,  0.0 ), float3( 0,0,1 ), ( ( _ShineRotation / 180.0 ) * UNITY_PI ) );
                float temp_output_103_0_g13208 = ( _ShineFrequency * _ShineWidth );
                float clampResult80_g13208 = clamp( ( ( ( sin( ( rotatedValue69_g13208.x - ( temp_output_40_0_g13207 * _ShineSpeed * _ShineFrequency ) ) ) - ( 1.0 - temp_output_103_0_g13208 ) ) / temp_output_103_0_g13208 ) * _ShineSmooth ) , 0.0 , 1.0 );
                float2 uv_ShineMask = IN.texcoord.xy * _ShineMask_ST.xy + _ShineMask_ST.zw;
                float4 tex2DNode3_g13209 = tex2D( _ShineMask, uv_ShineMask );
                #ifdef _SHINEMASKTOGGLE_ON
                float staticSwitch98_g13208 = ( _ShineFade * ( tex2DNode3_g13209.r * tex2DNode3_g13209.a ) );
                #else
                float staticSwitch98_g13208 = _ShineFade;
                #endif
                float4 appendResult8_g13208 = (float4(( temp_output_57_0_g13208 + ( ( pow( max( lerpResult92_g13208 , float3( 0.0001,0.0001,0.0001 ) ) , temp_cast_74 ) * (_ShineColor).rgb ) * clampResult80_g13208 * staticSwitch98_g13208 ) ) , (temp_output_1_0_g13208).a));
                #ifdef _ENABLESHINE_ON
                float4 staticSwitch36_g13207 = appendResult8_g13208;
                #else
                float4 staticSwitch36_g13207 = staticSwitch34_g13207;
                #endif
                float temp_output_41_0_g13243 = temp_output_40_0_g13207;
                float linValue16_g13245 = tex2D( _UberNoiseTexture, ( ( ( temp_output_41_0_g13243 * _PoisonNoiseSpeed ) + temp_output_41_0_g13207 ) * _PoisonNoiseScale ) ).r;
                float localMyCustomExpression16_g13245 = MyCustomExpression16_g13245( linValue16_g13245 );
                float3 temp_output_24_0_g13243 = (_PoisonColor).rgb;
                float4 temp_output_1_0_g13243 = staticSwitch36_g13207;
                float3 temp_output_28_0_g13243 = (temp_output_1_0_g13243).rgb;
                float4 break2_g13244 = float4( temp_output_28_0_g13243 , 0.0 );
                float3 lerpResult32_g13243 = lerp( temp_output_28_0_g13243 , ( temp_output_24_0_g13243 * ( ( break2_g13244.x + break2_g13244.x + break2_g13244.y + break2_g13244.y + break2_g13244.y + break2_g13244.z ) / 6.0 ) ) , ( _PoisonFade * _PoisonRecolorFactor ));
                float4 appendResult27_g13243 = (float4(( ( max( pow( max( abs( ( ( ( localMyCustomExpression16_g13245 + ( temp_output_41_0_g13243 * _PoisonShiftSpeed ) ) % 1.0 ) + -0.5 ) ) , 0.0001 ) , max( _PoisonDensity , 0.0001 ) ) , 0.0 ) * temp_output_24_0_g13243 * _PoisonFade * _PoisonNoiseBrightness ) + lerpResult32_g13243 ) , temp_output_1_0_g13243.a));
                #ifdef _ENABLEPOISON_ON
                float4 staticSwitch39_g13207 = appendResult27_g13243;
                #else
                float4 staticSwitch39_g13207 = staticSwitch36_g13207;
                #endif
                float4 temp_output_10_0_g13247 = staticSwitch39_g13207;
                float3 temp_output_12_0_g13247 = (temp_output_10_0_g13247).rgb;
                float2 temp_output_2_0_g13247 = temp_output_41_0_g13207;
                float temp_output_1_0_g13247 = temp_output_40_0_g13207;
                float2 temp_output_6_0_g13247 = ( temp_output_1_0_g13247 * _EnchantedSpeed );
                float linValue16_g13250 = tex2D( _UberNoiseTexture, ( ( ( temp_output_2_0_g13247 - ( ( temp_output_6_0_g13247 + float2( 1.234,5.6789 ) ) * float2( 0.95,1.05 ) ) ) * _EnchantedScale ) * float2( 1,1 ) ) ).r;
                float localMyCustomExpression16_g13250 = MyCustomExpression16_g13250( linValue16_g13250 );
                float linValue16_g13248 = tex2D( _UberNoiseTexture, ( ( ( temp_output_6_0_g13247 + temp_output_2_0_g13247 ) * _EnchantedScale ) * float2( 1,1 ) ) ).r;
                float localMyCustomExpression16_g13248 = MyCustomExpression16_g13248( linValue16_g13248 );
                float temp_output_36_0_g13247 = ( localMyCustomExpression16_g13250 + localMyCustomExpression16_g13248 );
                float temp_output_43_0_g13247 = ( temp_output_36_0_g13247 * 0.5 );
                float3 lerpResult42_g13247 = lerp( (_EnchantedLowColor).rgb , (_EnchantedHighColor).rgb , temp_output_43_0_g13247);
                float3 hsvTorgb53_g13247 = HSVToRGB( float3(( ( temp_output_43_0_g13247 * _EnchantedRainbowDensity ) + ( _EnchantedRainbowSpeed * temp_output_1_0_g13247 ) ),_EnchantedRainbowSaturation,1.0) );
                #ifdef _ENCHANTEDRAINBOWTOGGLE_ON
                float3 staticSwitch50_g13247 = hsvTorgb53_g13247;
                #else
                float3 staticSwitch50_g13247 = lerpResult42_g13247;
                #endif
                float4 break2_g13249 = temp_output_10_0_g13247;
                float3 temp_output_40_0_g13247 = ( staticSwitch50_g13247 * pow( max( ( ( break2_g13249.x + break2_g13249.x + break2_g13249.y + break2_g13249.y + break2_g13249.y + break2_g13249.z ) / 6.0 ) , 0.0001 ) , max( _EnchantedContrast , 0.0001 ) ) * _EnchantedBrightness );
                float temp_output_45_0_g13247 = ( max( ( temp_output_36_0_g13247 - _EnchantedReduce ) , 0.0 ) * _EnchantedFade );
                float3 lerpResult44_g13247 = lerp( temp_output_12_0_g13247 , temp_output_40_0_g13247 , temp_output_45_0_g13247);
                #ifdef _ENCHANTEDLERPTOGGLE_ON
                float3 staticSwitch47_g13247 = lerpResult44_g13247;
                #else
                float3 staticSwitch47_g13247 = ( temp_output_12_0_g13247 + ( temp_output_40_0_g13247 * temp_output_45_0_g13247 ) );
                #endif
                float4 appendResult19_g13247 = (float4(staticSwitch47_g13247 , temp_output_10_0_g13247.a));
                #ifdef _ENABLEENCHANTED_ON
                float4 staticSwitch11_g13247 = appendResult19_g13247;
                #else
                float4 staticSwitch11_g13247 = temp_output_10_0_g13247;
                #endif
                float4 temp_output_1_0_g13252 = staticSwitch11_g13247;
                float4 break5_g13252 = temp_output_1_0_g13252;
                float3 appendResult32_g13252 = (float3(break5_g13252.r , break5_g13252.g , break5_g13252.b));
                float4 break2_g13253 = temp_output_1_0_g13252;
                float temp_output_4_0_g13252 = ( ( break2_g13253.x + break2_g13253.x + break2_g13253.y + break2_g13253.y + break2_g13253.y + break2_g13253.z ) / 6.0 );
                float temp_output_11_0_g13252 = ( ( ( temp_output_4_0_g13252 + ( temp_output_40_0_g13207 * _ShiftingSpeed ) ) * _ShiftingDensity ) % 1.0 );
                float3 lerpResult20_g13252 = lerp( (_ShiftingColorA).rgb , (_ShiftingColorB).rgb , ( abs( ( temp_output_11_0_g13252 - 0.5 ) ) * 2.0 ));
                float3 hsvTorgb12_g13252 = HSVToRGB( float3(temp_output_11_0_g13252,_ShiftingSaturation,_ShiftingBrightness) );
                #ifdef _SHIFTINGRAINBOWTOGGLE_ON
                float3 staticSwitch26_g13252 = hsvTorgb12_g13252;
                #else
                float3 staticSwitch26_g13252 = ( lerpResult20_g13252 * _ShiftingBrightness );
                #endif
                float3 lerpResult31_g13252 = lerp( appendResult32_g13252 , ( staticSwitch26_g13252 * pow( max( temp_output_4_0_g13252 , 0.0001 ) , max( _ShiftingContrast , 0.0001 ) ) ) , _ShiftingFade);
                float4 appendResult6_g13252 = (float4(lerpResult31_g13252 , break5_g13252.a));
                #ifdef _ENABLESHIFTING_ON
                float4 staticSwitch33_g13252 = appendResult6_g13252;
                #else
                float4 staticSwitch33_g13252 = temp_output_1_0_g13252;
                #endif
                float4 temp_output_5_0_g13259 = staticSwitch33_g13252;
                float4 break6_g13259 = temp_output_5_0_g13259;
                float3 appendResult11_g13259 = (float3(break6_g13259.r , break6_g13259.g , break6_g13259.b));
                float temp_output_27_0_g13259 = temp_output_40_0_g13207;
                #ifdef _TEXTURELAYER1SCROLLTOGGLE_ON
                float2 staticSwitch73_g13259 = ( _TextureLayer1Offset + ( _TextureLayer1ScrollSpeed * temp_output_27_0_g13259 ) );
                #else
                float2 staticSwitch73_g13259 = _TextureLayer1Offset;
                #endif
                float2 temp_output_72_0_g13259 = ( ( _TextureLayer1Scale * temp_output_41_0_g13207 ) - staticSwitch73_g13259 );
                float2 temp_cast_83 = (_TextureLayer1EdgeClip).xx;
                float2 UV10_g13260 = ( ( ( ( ( ( ( ( ( temp_output_72_0_g13259 % float2( 1,1 ) ) + float2( 1,1 ) ) % float2( 1,1 ) ) - float2( 0.5,0.5 ) ) * ( float2( 1,1 ) - temp_cast_83 ) ) + float2( 0.5,0.5 ) ) % float2( 1,1 ) ) + float2( 1,1 ) ) % float2( 1,1 ) );
                int Width10_g13260 = _TextureLayer1Columns;
                int Height10_g13260 = _TextureLayer1Rows;
                float Tile10_g13260 = round( ( ( _TextureLayer1Speed * temp_output_27_0_g13259 ) + _TextureLayer1StartFrame ) );
                float2 Invert10_g13260 = float2( 0,1 );
                float2 localMyCustomExpression10_g13260 = MyCustomExpression10_g13260( UV10_g13260 , Width10_g13260 , Height10_g13260 , Tile10_g13260 , Invert10_g13260 );
                #ifdef _TEXTURELAYER1SHEETTOGGLE_ON
                float2 staticSwitch20_g13259 = localMyCustomExpression10_g13260;
                #else
                float2 staticSwitch20_g13259 = temp_output_72_0_g13259;
                #endif
                float4 tex2DNode3_g13259 = tex2D( _TextureLayer1Texture, staticSwitch20_g13259 );
                float3 appendResult13_g13259 = (float3(tex2DNode3_g13259.r , tex2DNode3_g13259.g , tex2DNode3_g13259.b));
                float3 appendResult18_g13259 = (float3(_TextureLayer1Color.r , _TextureLayer1Color.g , _TextureLayer1Color.b));
                float3 temp_output_16_0_g13259 = ( appendResult13_g13259 * appendResult18_g13259 );
                float4 break2_g13261 = temp_output_5_0_g13259;
                #ifdef _TEXTURELAYER1CONTRASTTOGGLE_ON
                float3 staticSwitch80_g13259 = ( pow( max( ( ( break2_g13261.x + break2_g13261.x + break2_g13261.y + break2_g13261.y + break2_g13261.y + break2_g13261.z ) / 6.0 ) , 0.0001 ) , max( _TextureLayer1Contrast , 0.0001 ) ) * temp_output_16_0_g13259 );
                #else
                float3 staticSwitch80_g13259 = temp_output_16_0_g13259;
                #endif
                float3 lerpResult12_g13259 = lerp( appendResult11_g13259 , staticSwitch80_g13259 , ( tex2DNode3_g13259.a * _TextureLayer1Fade ));
                float4 appendResult14_g13259 = (float4(lerpResult12_g13259 , break6_g13259.a));
                #ifdef _ENABLETEXTURELAYER1_ON
                float4 staticSwitch15_g13259 = appendResult14_g13259;
                #else
                float4 staticSwitch15_g13259 = temp_output_5_0_g13259;
                #endif
                float4 temp_output_5_0_g13255 = staticSwitch15_g13259;
                float4 break6_g13255 = temp_output_5_0_g13255;
                float3 appendResult11_g13255 = (float3(break6_g13255.r , break6_g13255.g , break6_g13255.b));
                float temp_output_27_0_g13255 = temp_output_40_0_g13207;
                #ifdef _TEXTURELAYER2SCROLLTOGGLE_ON
                float2 staticSwitch73_g13255 = ( _TextureLayer2Offset + ( _TextureLayer2ScrollSpeed * temp_output_27_0_g13255 ) );
                #else
                float2 staticSwitch73_g13255 = _TextureLayer2Offset;
                #endif
                float2 temp_output_72_0_g13255 = ( ( _TextureLayer2Scale * temp_output_41_0_g13207 ) - staticSwitch73_g13255 );
                float2 temp_cast_86 = (_TextureLayer2EdgeClip).xx;
                float2 UV10_g13256 = ( ( ( ( ( ( ( ( ( temp_output_72_0_g13255 % float2( 1,1 ) ) + float2( 1,1 ) ) % float2( 1,1 ) ) - float2( 0.5,0.5 ) ) * ( float2( 1,1 ) - temp_cast_86 ) ) + float2( 0.5,0.5 ) ) % float2( 1,1 ) ) + float2( 1,1 ) ) % float2( 1,1 ) );
                int Width10_g13256 = _TextureLayer2Columns;
                int Height10_g13256 = _TextureLayer2Rows;
                float Tile10_g13256 = round( ( ( _TextureLayer2Speed * temp_output_27_0_g13255 ) + _TextureLayer2StartFrame ) );
                float2 Invert10_g13256 = float2( 0,1 );
                float2 localMyCustomExpression10_g13256 = MyCustomExpression10_g13256( UV10_g13256 , Width10_g13256 , Height10_g13256 , Tile10_g13256 , Invert10_g13256 );
                #ifdef _TEXTURELAYER2SHEETTOGGLE_ON
                float2 staticSwitch20_g13255 = localMyCustomExpression10_g13256;
                #else
                float2 staticSwitch20_g13255 = temp_output_72_0_g13255;
                #endif
                float4 tex2DNode3_g13255 = tex2D( _TextureLayer2Texture, staticSwitch20_g13255 );
                float3 appendResult13_g13255 = (float3(tex2DNode3_g13255.r , tex2DNode3_g13255.g , tex2DNode3_g13255.b));
                float3 appendResult18_g13255 = (float3(_TextureLayer2Color.r , _TextureLayer2Color.g , _TextureLayer2Color.b));
                float3 temp_output_16_0_g13255 = ( appendResult13_g13255 * appendResult18_g13255 );
                float4 break2_g13257 = temp_output_5_0_g13255;
                #ifdef _TEXTURELAYER2CONTRASTTOGGLE_ON
                float3 staticSwitch84_g13255 = ( pow( max( ( ( break2_g13257.x + break2_g13257.x + break2_g13257.y + break2_g13257.y + break2_g13257.y + break2_g13257.z ) / 6.0 ) , 0.0001 ) , max( _TextureLayer2Contrast , 0.0001 ) ) * temp_output_16_0_g13255 );
                #else
                float3 staticSwitch84_g13255 = temp_output_16_0_g13255;
                #endif
                float3 lerpResult12_g13255 = lerp( appendResult11_g13255 , staticSwitch84_g13255 , ( tex2DNode3_g13255.a * _TextureLayer2Fade ));
                float4 appendResult14_g13255 = (float4(lerpResult12_g13255 , break6_g13255.a));
                #ifdef _ENABLETEXTURELAYER2_ON
                float4 staticSwitch15_g13255 = appendResult14_g13255;
                #else
                float4 staticSwitch15_g13255 = temp_output_5_0_g13255;
                #endif
                float4 temp_output_473_0 = staticSwitch15_g13255;
                float4 break4_g13263 = temp_output_473_0;
                float fullDistortionAlpha164 = _FullDistortionFade;
                float4 appendResult5_g13263 = (float4(break4_g13263.r , break4_g13263.g , break4_g13263.b , ( break4_g13263.a * fullDistortionAlpha164 )));
                #ifdef _ENABLEFULLDISTORTION_ON
                float4 staticSwitch77 = appendResult5_g13263;
                #else
                float4 staticSwitch77 = temp_output_473_0;
                #endif
                float4 break4_g13264 = staticSwitch77;
                float directionalDistortionAlpha167 = (( _DirectionalDistortionInvert )?( ( 1.0 - clampResult154_g13115 ) ):( clampResult154_g13115 ));
                float4 appendResult5_g13264 = (float4(break4_g13264.r , break4_g13264.g , break4_g13264.b , ( break4_g13264.a * directionalDistortionAlpha167 )));
                #ifdef _ENABLEDIRECTIONALDISTORTION_ON
                float4 staticSwitch75 = appendResult5_g13264;
                #else
                float4 staticSwitch75 = staticSwitch77;
                #endif
                float4 temp_output_1_0_g13265 = staticSwitch75;
                float4 temp_output_1_0_g13266 = temp_output_1_0_g13265;
                float temp_output_53_0_g13266 = max( _FullAlphaDissolveWidth , 0.001 );
                float2 temp_output_18_0_g13265 = shaderPosition235;
                float linValue16_g13267 = tex2D( _UberNoiseTexture, ( temp_output_18_0_g13265 * _FullAlphaDissolveNoiseScale ) ).r;
                float localMyCustomExpression16_g13267 = MyCustomExpression16_g13267( linValue16_g13267 );
                float clampResult17_g13266 = clamp( ( ( ( _FullAlphaDissolveFade * ( 1.0 + temp_output_53_0_g13266 ) ) - localMyCustomExpression16_g13267 ) / temp_output_53_0_g13266 ) , 0.0 , 1.0 );
                float4 appendResult3_g13266 = (float4((temp_output_1_0_g13266).rgb , ( temp_output_1_0_g13266.a * clampResult17_g13266 )));
                #ifdef _ENABLEFULLALPHADISSOLVE_ON
                float4 staticSwitch3_g13265 = appendResult3_g13266;
                #else
                float4 staticSwitch3_g13265 = temp_output_1_0_g13265;
                #endif
                float linValue16_g13275 = tex2D( _UberNoiseTexture, ( temp_output_18_0_g13265 * _FullGlowDissolveNoiseScale ) ).r;
                float localMyCustomExpression16_g13275 = MyCustomExpression16_g13275( linValue16_g13275 );
                float temp_output_5_0_g13274 = localMyCustomExpression16_g13275;
                float temp_output_61_0_g13274 = step( temp_output_5_0_g13274 , _FullGlowDissolveFade );
                float temp_output_53_0_g13274 = max( ( _FullGlowDissolveFade * _FullGlowDissolveWidth ) , 0.001 );
                float4 temp_output_1_0_g13274 = staticSwitch3_g13265;
                float4 appendResult3_g13274 = (float4(( ( (_FullGlowDissolveEdgeColor).rgb * ( temp_output_61_0_g13274 - step( temp_output_5_0_g13274 , ( ( _FullGlowDissolveFade * ( 1.01 + temp_output_53_0_g13274 ) ) - temp_output_53_0_g13274 ) ) ) ) + (temp_output_1_0_g13274).rgb ) , ( temp_output_1_0_g13274.a * temp_output_61_0_g13274 )));
                #ifdef _ENABLEFULLGLOWDISSOLVE_ON
                float4 staticSwitch5_g13265 = appendResult3_g13274;
                #else
                float4 staticSwitch5_g13265 = staticSwitch3_g13265;
                #endif
                float4 temp_output_1_0_g13276 = staticSwitch5_g13265;
                float2 temp_output_76_0_g13276 = temp_output_18_0_g13265;
                float linValue16_g13277 = tex2D( _UberNoiseTexture, ( temp_output_76_0_g13276 * _SourceAlphaDissolveNoiseScale ) ).r;
                float localMyCustomExpression16_g13277 = MyCustomExpression16_g13277( linValue16_g13277 );
                float clampResult17_g13276 = clamp( ( ( _SourceAlphaDissolveFade - ( distance( _SourceAlphaDissolvePosition , temp_output_76_0_g13276 ) + ( localMyCustomExpression16_g13277 * _SourceAlphaDissolveNoiseFactor ) ) ) / max( _SourceAlphaDissolveWidth , 0.001 ) ) , 0.0 , 1.0 );
                float4 appendResult3_g13276 = (float4((temp_output_1_0_g13276).rgb , ( temp_output_1_0_g13276.a * (( _SourceAlphaDissolveInvert )?( ( 1.0 - clampResult17_g13276 ) ):( clampResult17_g13276 )) )));
                #ifdef _ENABLESOURCEALPHADISSOLVE_ON
                float4 staticSwitch8_g13265 = appendResult3_g13276;
                #else
                float4 staticSwitch8_g13265 = staticSwitch5_g13265;
                #endif
                float2 temp_output_90_0_g13272 = temp_output_18_0_g13265;
                float linValue16_g13273 = tex2D( _UberNoiseTexture, ( temp_output_90_0_g13272 * _SourceGlowDissolveNoiseScale ) ).r;
                float localMyCustomExpression16_g13273 = MyCustomExpression16_g13273( linValue16_g13273 );
                float temp_output_65_0_g13272 = ( distance( _SourceGlowDissolvePosition , temp_output_90_0_g13272 ) + ( localMyCustomExpression16_g13273 * _SourceGlowDissolveNoiseFactor ) );
                float temp_output_75_0_g13272 = step( temp_output_65_0_g13272 , _SourceGlowDissolveFade );
                float temp_output_76_0_g13272 = step( temp_output_65_0_g13272 , ( _SourceGlowDissolveFade - max( _SourceGlowDissolveWidth , 0.001 ) ) );
                float4 temp_output_1_0_g13272 = staticSwitch8_g13265;
                float4 appendResult3_g13272 = (float4(( ( max( ( temp_output_75_0_g13272 - temp_output_76_0_g13272 ) , 0.0 ) * (_SourceGlowDissolveEdgeColor).rgb ) + (temp_output_1_0_g13272).rgb ) , ( temp_output_1_0_g13272.a * (( _SourceGlowDissolveInvert )?( ( 1.0 - temp_output_76_0_g13272 ) ):( temp_output_75_0_g13272 )) )));
                #ifdef _ENABLESOURCEGLOWDISSOLVE_ON
                float4 staticSwitch9_g13265 = appendResult3_g13272;
                #else
                float4 staticSwitch9_g13265 = staticSwitch8_g13265;
                #endif
                float4 temp_output_1_0_g13268 = staticSwitch9_g13265;
                float2 temp_output_161_0_g13268 = temp_output_18_0_g13265;
                float3 rotatedValue136_g13268 = RotateAroundAxis( float3( 0,0,0 ), float3( temp_output_161_0_g13268 ,  0.0 ), float3( 0,0,1 ), ( ( ( _DirectionalAlphaFadeRotation / 180.0 ) + -0.25 ) * UNITY_PI ) );
                float3 break130_g13268 = rotatedValue136_g13268;
                float linValue16_g13269 = tex2D( _UberNoiseTexture, ( temp_output_161_0_g13268 * _DirectionalAlphaFadeNoiseScale ) ).r;
                float localMyCustomExpression16_g13269 = MyCustomExpression16_g13269( linValue16_g13269 );
                float clampResult154_g13268 = clamp( ( ( break130_g13268.x + break130_g13268.y + _DirectionalAlphaFadeFade + ( localMyCustomExpression16_g13269 * _DirectionalAlphaFadeNoiseFactor ) ) / max( _DirectionalAlphaFadeWidth , 0.001 ) ) , 0.0 , 1.0 );
                float4 appendResult3_g13268 = (float4((temp_output_1_0_g13268).rgb , ( temp_output_1_0_g13268.a * (( _DirectionalAlphaFadeInvert )?( ( 1.0 - clampResult154_g13268 ) ):( clampResult154_g13268 )) )));
                #ifdef _ENABLEDIRECTIONALALPHAFADE_ON
                float4 staticSwitch11_g13265 = appendResult3_g13268;
                #else
                float4 staticSwitch11_g13265 = staticSwitch9_g13265;
                #endif
                float2 temp_output_171_0_g13270 = temp_output_18_0_g13265;
                float3 rotatedValue136_g13270 = RotateAroundAxis( float3( 0,0,0 ), float3( temp_output_171_0_g13270 ,  0.0 ), float3( 0,0,1 ), ( ( ( _DirectionalGlowFadeRotation / 180.0 ) + -0.25 ) * UNITY_PI ) );
                float3 break130_g13270 = rotatedValue136_g13270;
                float linValue16_g13271 = tex2D( _UberNoiseTexture, ( temp_output_171_0_g13270 * _DirectionalGlowFadeNoiseScale ) ).r;
                float localMyCustomExpression16_g13271 = MyCustomExpression16_g13271( linValue16_g13271 );
                float temp_output_168_0_g13270 = max( ( ( break130_g13270.x + break130_g13270.y + _DirectionalGlowFadeFade + ( localMyCustomExpression16_g13271 * _DirectionalGlowFadeNoiseFactor ) ) / max( _DirectionalGlowFadeWidth , 0.001 ) ) , 0.0 );
                float temp_output_161_0_g13270 = step( 0.1 , (( _DirectionalGlowFadeInvert )?( ( 1.0 - temp_output_168_0_g13270 ) ):( temp_output_168_0_g13270 )) );
                float4 temp_output_1_0_g13270 = staticSwitch11_g13265;
                float clampResult154_g13270 = clamp( temp_output_161_0_g13270 , 0.0 , 1.0 );
                float4 appendResult3_g13270 = (float4(( ( (_DirectionalGlowFadeEdgeColor).rgb * ( temp_output_161_0_g13270 - step( 1.0 , (( _DirectionalGlowFadeInvert )?( ( 1.0 - temp_output_168_0_g13270 ) ):( temp_output_168_0_g13270 )) ) ) ) + (temp_output_1_0_g13270).rgb ) , ( temp_output_1_0_g13270.a * clampResult154_g13270 )));
                #ifdef _ENABLEDIRECTIONALGLOWFADE_ON
                float4 staticSwitch15_g13265 = appendResult3_g13270;
                #else
                float4 staticSwitch15_g13265 = staticSwitch11_g13265;
                #endif
                float4 temp_output_1_0_g13278 = staticSwitch15_g13265;
                float2 temp_output_126_0_g13278 = temp_output_18_0_g13265;
                float temp_output_121_0_g13278 = max( ( ( _HalftoneFade - distance( _HalftonePosition , temp_output_126_0_g13278 ) ) / max( 0.01 , _HalftoneFadeWidth ) ) , 0.0 );
                float2 appendResult11_g13279 = (float2(temp_output_121_0_g13278 , temp_output_121_0_g13278));
                float temp_output_17_0_g13279 = length( ( (( ( abs( temp_output_126_0_g13278 ) * _HalftoneTiling ) % float2( 1,1 ) )*2.0 + -1.0) / appendResult11_g13279 ) );
                float clampResult17_g13278 = clamp( saturate( ( ( 1.0 - temp_output_17_0_g13279 ) / fwidth( temp_output_17_0_g13279 ) ) ) , 0.0 , 1.0 );
                float4 appendResult3_g13278 = (float4((temp_output_1_0_g13278).rgb , ( temp_output_1_0_g13278.a * (( _HalftoneInvert )?( ( 1.0 - clampResult17_g13278 ) ):( clampResult17_g13278 )) )));
                #ifdef _ENABLEHALFTONE_ON
                float4 staticSwitch13_g13265 = appendResult3_g13278;
                #else
                float4 staticSwitch13_g13265 = staticSwitch15_g13265;
                #endif
                float3 temp_output_3_0_g13281 = (_AddColorColor).rgb;
                float2 uv_AddColorMask = IN.texcoord.xy * _AddColorMask_ST.xy + _AddColorMask_ST.zw;
                float4 tex2DNode19_g13281 = tex2D( _AddColorMask, uv_AddColorMask );
                #ifdef _ADDCOLORMASKTOGGLE_ON
                float3 staticSwitch16_g13281 = ( temp_output_3_0_g13281 * ( (tex2DNode19_g13281).rgb * tex2DNode19_g13281.a ) );
                #else
                float3 staticSwitch16_g13281 = temp_output_3_0_g13281;
                #endif
                float4 temp_output_1_0_g13281 = staticSwitch13_g13265;
                float4 break2_g13283 = temp_output_1_0_g13281;
                #ifdef _ADDCOLORCONTRASTTOGGLE_ON
                float3 staticSwitch17_g13281 = ( staticSwitch16_g13281 * pow( max( ( ( break2_g13283.x + break2_g13283.x + break2_g13283.y + break2_g13283.y + break2_g13283.y + break2_g13283.z ) / 6.0 ) , 0.0001 ) , max( _AddColorContrast , 0.0001 ) ) );
                #else
                float3 staticSwitch17_g13281 = staticSwitch16_g13281;
                #endif
                float4 appendResult6_g13281 = (float4(( ( staticSwitch17_g13281 * _AddColorFade ) + (temp_output_1_0_g13281).rgb ) , temp_output_1_0_g13281.a));
                #ifdef _ENABLEADDCOLOR_ON
                float4 staticSwitch5_g13280 = appendResult6_g13281;
                #else
                float4 staticSwitch5_g13280 = staticSwitch13_g13265;
                #endif
                float4 temp_output_1_0_g13284 = staticSwitch5_g13280;
                float3 lerpResult4_g13284 = lerp( (temp_output_1_0_g13284).rgb , (_AlphaTintColor).rgb , ( ( 1.0 - temp_output_1_0_g13284.a ) * step( _AlphaTintMinAlpha , temp_output_1_0_g13284.a ) * _AlphaTintFade ));
                float4 appendResult13_g13284 = (float4(lerpResult4_g13284 , temp_output_1_0_g13284.a));
                #ifdef _ENABLEALPHATINT_ON
                float4 staticSwitch11_g13280 = appendResult13_g13284;
                #else
                float4 staticSwitch11_g13280 = staticSwitch5_g13280;
                #endif
                float4 temp_output_1_0_g13285 = staticSwitch11_g13280;
                float3 temp_output_6_0_g13285 = (_StrongTintTint).rgb;
                float2 uv_StrongTintMask = IN.texcoord.xy * _StrongTintMask_ST.xy + _StrongTintMask_ST.zw;
                float4 tex2DNode23_g13285 = tex2D( _StrongTintMask, uv_StrongTintMask );
                #ifdef _STRONGTINTMASKTOGGLE_ON
                float3 staticSwitch21_g13285 = ( temp_output_6_0_g13285 * ( (tex2DNode23_g13285).rgb * tex2DNode23_g13285.a ) );
                #else
                float3 staticSwitch21_g13285 = temp_output_6_0_g13285;
                #endif
                float4 break2_g13287 = temp_output_1_0_g13285;
                #ifdef _STRONGTINTCONTRASTTOGGLE_ON
                float3 staticSwitch22_g13285 = ( pow( max( ( ( break2_g13287.x + break2_g13287.x + break2_g13287.y + break2_g13287.y + break2_g13287.y + break2_g13287.z ) / 6.0 ) , 0.0001 ) , max( _StrongTintContrast , 0.0001 ) ) * staticSwitch21_g13285 );
                #else
                float3 staticSwitch22_g13285 = staticSwitch21_g13285;
                #endif
                float3 lerpResult7_g13285 = lerp( (temp_output_1_0_g13285).rgb , staticSwitch22_g13285 , _StrongTintFade);
                float4 appendResult9_g13285 = (float4(lerpResult7_g13285 , (temp_output_1_0_g13285).a));
                #ifdef _ENABLESTRONGTINT_ON
                float4 staticSwitch7_g13280 = appendResult9_g13285;
                #else
                float4 staticSwitch7_g13280 = staticSwitch11_g13280;
                #endif
                float4 temp_output_2_0_g13288 = staticSwitch7_g13280;
                float4 break4_g13290 = temp_output_2_0_g13288;
                float3 appendResult5_g13290 = (float3(break4_g13290.r , break4_g13290.g , break4_g13290.b));
                float2 appendResult10_g13289 = (float2(_MainTex_TexelSize.z , _MainTex_TexelSize.w));
                float2 appendResult2_g13289 = (float2(1.0 , ( _MainTex_TexelSize.z / _MainTex_TexelSize.w )));
                #ifdef _RESIZABLETEXTURE_ON
                float2 staticSwitch9_g13289 = appendResult2_g13289;
                #else
                float2 staticSwitch9_g13289 = ( float2( 100,100 ) / appendResult10_g13289 );
                #endif
                float4 appendResult85_g13288 = (float4(_ShadowColor.r , _ShadowColor.g , _ShadowColor.b , ( _ShadowFade * tex2D( _MainTex, ( finalUV146 - ( staticSwitch9_g13289 * _ShadowOffset ) ) ).a )));
                float4 break6_g13290 = appendResult85_g13288;
                float3 appendResult7_g13290 = (float3(break6_g13290.r , break6_g13290.g , break6_g13290.b));
                float temp_output_11_0_g13290 = ( ( 1.0 - break4_g13290.a ) * break6_g13290.a );
                float temp_output_32_0_g13290 = ( break4_g13290.a + temp_output_11_0_g13290 );
                float4 appendResult18_g13290 = (float4(( ( ( appendResult5_g13290 * break4_g13290.a ) + ( appendResult7_g13290 * temp_output_11_0_g13290 ) ) * ( 1.0 / max( temp_output_32_0_g13290 , 0.01 ) ) ) , temp_output_32_0_g13290));
                #ifdef _ENABLESHADOW_ON
                float4 staticSwitch82_g13288 = appendResult18_g13290;
                #else
                float4 staticSwitch82_g13288 = temp_output_2_0_g13288;
                #endif
                float4 break4_g13291 = staticSwitch82_g13288;
                #ifdef _ENABLECUSTOMFADE_ON
                float staticSwitch8_g13135 = 1.0;
                #else
                float staticSwitch8_g13135 = IN.color.a;
                #endif
                #ifdef _ENABLESMOKE_ON
                float staticSwitch9_g13135 = 1.0;
                #else
                float staticSwitch9_g13135 = staticSwitch8_g13135;
                #endif
                float customVertexAlpha193 = staticSwitch9_g13135;
                float4 appendResult5_g13291 = (float4(break4_g13291.r , break4_g13291.g , break4_g13291.b , ( break4_g13291.a * customVertexAlpha193 )));
                float4 temp_output_344_0 = appendResult5_g13291;
                float4 temp_output_1_0_g13293 = temp_output_344_0;
                float4 appendResult8_g13293 = (float4(( (temp_output_1_0_g13293).rgb * (UIVertexColor514).rgb ) , temp_output_1_0_g13293.a));
                #ifdef _VERTEXTINTFIRST_ON
                float4 staticSwitch342 = temp_output_344_0;
                #else
                float4 staticSwitch342 = appendResult8_g13293;
                #endif
                float4 temp_output_1_0_g13294 = originalColor191;
                float4 appendResult8_g13294 = (float4(( (temp_output_1_0_g13294).rgb * (UIVertexColor514).rgb ) , temp_output_1_0_g13294.a));
                float4 lerpResult125 = lerp( appendResult8_g13294 , staticSwitch342 , fullFade123);
                #if defined( _SHADERFADING_NONE )
                float4 staticSwitch143 = staticSwitch342;
                #elif defined( _SHADERFADING_FULL )
                float4 staticSwitch143 = lerpResult125;
                #elif defined( _SHADERFADING_MASK )
                float4 staticSwitch143 = lerpResult125;
                #elif defined( _SHADERFADING_DISSOLVE )
                float4 staticSwitch143 = lerpResult125;
                #elif defined( _SHADERFADING_SPREAD )
                float4 staticSwitch143 = lerpResult125;
                #else
                float4 staticSwitch143 = staticSwitch342;
                #endif
                float4 temp_output_7_0_g13299 = staticSwitch143;
                float4 appendResult2_g13299 = (float4(( (temp_output_7_0_g13299).rgb / max( temp_output_7_0_g13299.a , 1E-05 ) ) , temp_output_7_0_g13299.a));
                #ifdef _BAKEDMATERIAL_ON
                float4 staticSwitch6_g13299 = appendResult2_g13299;
                #else
                float4 staticSwitch6_g13299 = temp_output_7_0_g13299;
                #endif
                

                half4 color = staticSwitch6_g13299;

                #ifdef UNITY_UI_CLIP_RECT
                half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
                color.a *= m.x * m.y;
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                color.rgb *= color.a;

                return color;
            }
        ENDCG
        }
    }
    CustomEditor "SpriteShadersUltimate.SSUShaderGUI"
	
	Fallback Off
}
/*ASEBEGIN
Version=19905
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;411;-3081.191,-3627.324;Inherit;False;157;spriteTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;363;-3105.9,-3835.053;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;483;-2758.191,-3709.324;Inherit;False;_ScreenTiling;620;;11656;f5939d1b891718b468aa402ddf2c75e0;0;2;1;FLOAT2;0,0;False;12;SAMPLER2D;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;482;-2458.77,-3622.03;Inherit;False;_WorldTiling;615;;11657;5075a3cd4854af640aa8d277732c8893;0;2;1;FLOAT2;0,0;False;12;SAMPLER2D;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;373;-2003.491,-3335.527;Inherit;False;Property;_SpriteSheetRect;Sprite Sheet Rect;10;0;Create;True;0;0;0;False;0;False;0,0,1,1;0,0,1,1;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.TemplateShaderPropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;30;1182.59,1001.108;Inherit;False;0;0;_MainTex;Shader;False;0;5;SAMPLER2D;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;460;-2186.147,-3620.607;Inherit;False;originalUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;469;-1956.314,-3488.134;Inherit;False;_PixelArtUV_1;-1;;11658;0e4f4d9760e013e4ea49a4cc7c42c155;0;2;1;FLOAT2;0,0;False;2;SAMPLER2D;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;374;-1729.456,-3326.936;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;375;-1731.491,-3207.527;Inherit;False;FLOAT2;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;105;1425.709,1004.581;Inherit;False;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;449;-1697.482,-3566.466;Inherit;False;Property;_Keyword1;Keyword 1;8;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;427;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;377;-1494.584,-3224.105;Inherit;False;spriteRectMax;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;376;-1493.884,-3326.405;Inherit;False;spriteRectMin;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;157;1638.393,1000.485;Inherit;False;spriteTexture;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;450;-1448.75,-3526.053;Inherit;False;uvAfterPixelArt;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;370;-1198.361,-3311.903;Inherit;False;Remap2D;-1;;11660;f79f855c0a5c94649b58f3d8127375ae;0;5;13;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;6;FLOAT2;1,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;101;2238.417,1562.63;Inherit;True;Property;_UberNoiseTexture;Uber Noise Texture;42;0;Create;True;0;0;0;False;0;False;b8d18cd117976254d94a812a0bfc336e;b8d18cd117976254d94a812a0bfc336e;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.TexelSizeNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;438;1931.978,1144.547;Inherit;False;-1;Create;1;0;SAMPLER2D;;False;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;366;-905.3608,-3353.903;Inherit;False;Property;_SpriteSheetFix;Sprite Sheet Fix;9;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;False;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;99;2506.903,1635.005;Inherit;False;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;435;2252.064,903.7132;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;432;2227.661,1200.654;Inherit;False;False;False;True;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;477;-582.6621,-3244.321;Inherit;False;237;shaderTime;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;475;-584.1226,-3343.809;Inherit;False;fixedUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;159;2660.32,1715.519;Inherit;False;uberNoiseTexture;-1;True;1;0;SAMPLER2D;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.IntNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;509;-2736,-32;Inherit;False;Property;_UIVertexColorAlwaysGammaSpace;UIVertexColorAlwaysGammaSpace;5;1;[HideInInspector];Create;True;0;0;0;True;0;False;0;0;False;0;1;INT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;434;2534.932,1066.922;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;365;-372.9774,-3275.82;Inherit;False;_UberInteractive;548;;11663;f8a4d7008519ad249b29e4a9381f963f;0;2;9;FLOAT;0;False;3;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;253;-334.9839,-3014.146;Inherit;False;235;shaderPosition;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;184;-340.3444,-2931.6;Inherit;False;159;uberNoiseTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;147;-2484.428,-386.3136;Inherit;False;146;finalUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;189;-2505.721,-234.4758;Inherit;False;157;spriteTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;513;-2400,-32;Inherit;False;UIVertex;-1;;13092;df2da7897c6c4a5489965ec6027965da;0;1;3;INT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.FloorOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;433;2745.127,1095.848;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;84;-178.5299,-3184.649;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;79;-47.4363,-2946.944;Inherit;False;_FullDistortion;480;;13093;62960fe27c1c398408207bb462ffd10e;0;3;195;FLOAT2;0,0;False;160;FLOAT2;0,0;False;194;SAMPLER2D;;False;2;FLOAT2;174;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;188;476.926,-2831.78;Inherit;False;159;uberNoiseTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;254;496.0161,-2937.146;Inherit;False;235;shaderPosition;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;160;658.3505,-580.0461;Inherit;False;159;uberNoiseTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;244;685.5539,-740.5018;Inherit;False;237;shaderTime;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;243;665.8691,-664.6964;Inherit;False;235;shaderPosition;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;471;-2266.589,-335.832;Inherit;False;_UberSample;586;;13096;1028d755b36e2b04da25c3b882a2e2ec;0;2;1;FLOAT2;0,0;False;2;SAMPLER2D;;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;514;-1984,-32;Inherit;False;UIVertexColor;-1;True;1;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;436;2945.127,1149.848;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;83;355.5295,-3077.305;Inherit;False;Property;_EnableShine;Enable Shine;479;0;Create;True;0;0;0;False;0;False;1;0;0;True;;Toggle;2;Key0;Key1;Reference;77;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;81;747.3577,-2918.135;Inherit;False;_DirectionalDistortion;468;;13115;30e6ac39427ee11419083602d572972f;0;3;182;FLOAT2;0,0;False;160;FLOAT2;0,0;False;181;SAMPLER2D;;False;2;FLOAT2;174;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;53;1165.579,-2580.498;Inherit;False;Property;_HologramFade;Hologram: Fade;211;0;Create;True;0;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;102;919.0109,-667.4209;Inherit;False;_GlitchPre;228;;13118;b8ad29d751d87bd4d9cbf14898be6163;0;3;19;FLOAT;0;False;18;FLOAT2;0,0;False;16;SAMPLER2D;;False;2;FLOAT;15;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;228;4626.359,2471.966;Inherit;False;159;uberNoiseTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;261;4535.53,1746.504;Inherit;False;235;shaderPosition;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;219;4515.66,1836.189;Inherit;False;159;uberNoiseTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;229;4660.594,2553.942;Inherit;False;Property;_FadingPosition;Fading: Position;26;0;Create;True;0;0;0;False;0;False;0,0;0.2,0.2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;208;4401.642,1937.264;Inherit;False;Property;_FadingNoiseScale;Fading: Noise Scale;29;0;Create;True;0;0;0;False;0;False;0.2,0.2;0.2,0.2;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;230;4630.415,2681.855;Inherit;False;Property;_FadingNoiseFactor;Fading: Noise Factor;28;0;Create;True;0;0;0;False;0;False;0.2;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.TexturePropertyNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;204;4801.255,1506.884;Inherit;True;Property;_FadingMask;Fading: Mask;30;0;Create;True;0;0;0;False;0;False;None;None;False;white;Auto;Texture2D;False;-1;0;2;SAMPLER2D;0;SAMPLERSTATE;1
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;210;4416.092,2114.768;Inherit;False;Property;_FadingWidth;Fading: Width;27;0;Create;True;0;0;0;False;0;False;0.3;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;494;-1944.149,-267.9059;Inherit;False;_GUIUber;0;;13120;c9d186c63c4d98140945bb82e35de84d;0;1;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;356;-1686.027,-106.7738;Inherit;False;ColorMultiply;-1;;13121;1f51da7edd80c06488c56d28bc096dec;0;2;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;441;3148.675,1260.326;Inherit;False;157;spriteTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;437;3093.581,1032.267;Inherit;False;Property;_PixelPerfectSpace;Pixel Perfect Space;7;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;False;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;82;1064.56,-3052.917;Inherit;False;Property;_EnableShine;Enable Shine;467;0;Create;True;0;0;0;False;0;False;1;0;0;True;;Toggle;2;Key0;Key1;Reference;75;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;174;1562.277,-2362.367;Inherit;False;157;spriteTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;255;1530.016,-2640.146;Inherit;False;237;shaderTime;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;173;1547.656,-2458.612;Inherit;False;159;uberNoiseTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;182;1537.252,-2554.561;Inherit;False;hologramFade;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;500;1584,-2208;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;499;1616,-2288;Inherit;False;Constant;_Float1;Float 1;102;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;122;3995.099,1279.389;Inherit;False;Property;_FadingFade;Fading: Fade;25;0;Create;True;0;0;0;False;0;False;1;0;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;152;1246.533,-755.1426;Inherit;False;glitchFade;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;154;1243.538,-600.6849;Inherit;False;glitchPosition;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;292;5091.561,1459.408;Inherit;False;ShaderMasker;-1;;13122;3d25b55dbfdd24f48b9bd371bdde0e97;0;2;1;FLOAT;0;False;2;SAMPLER2D;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;223;4905.316,1828.167;Inherit;False;_UberDissolveFade;-1;;13123;cb957eb9b67f4f243aa8ba0547208263;0;5;21;FLOAT2;0,0;False;1;FLOAT;0;False;16;SAMPLER2D;0;False;18;FLOAT2;0,0;False;20;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;231;4899.456,2440.854;Inherit;False;_UberSpreadFade;-1;;13125;777ca8ab10170fb48b24b7cd1c44f075;0;7;27;FLOAT2;0,0;False;22;FLOAT;0;False;18;SAMPLER2D;0;False;25;FLOAT2;0,0;False;23;FLOAT2;0,0;False;21;FLOAT;0;False;26;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;354;-1350.718,-125.6577;Inherit;False;Property;_Keyword0;Keyword 0;6;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;342;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;341;2716.61,1541.034;Inherit;False;ShaderTime;31;;13127;06a15e67904f217499045f361bad56e7;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;439;3389.449,1182.896;Inherit;False;ShaderSpace;15;;13128;be729ef05db9c224caec82a3516038dc;0;2;61;FLOAT2;0,0;False;3;SAMPLER2D;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RelayNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;38;1602.103,-2721.81;Inherit;False;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;507;1839.482,-2552.931;Inherit;False;_HologramUV;219;;13129;7c71b1b031ffcbe48805e17b94671163;0;7;77;FLOAT;0;False;55;FLOAT;0;False;76;SAMPLER2D;;False;37;FLOAT2;0,0;False;39;SAMPLER2D;;False;81;FLOAT;0;False;79;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;153;2413.1,-2367.982;Inherit;False;152;glitchFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;175;2375.652,-2434.015;Inherit;False;159;uberNoiseTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;155;2400.558,-2511.178;Inherit;False;154;glitchPosition;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;256;2394.016,-2741.146;Inherit;False;237;shaderTime;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;139;5964.391,1365.661;Inherit;False;Property;_ShaderFading;Shader Fading;24;0;Create;True;0;0;0;False;0;False;0;0;0;True;;KeywordEnum;5;None;Full;Mask;Dissolve;Spread;Create;True;True;All;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;149;-977.8615,164.14;Inherit;False;146;finalUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;191;-1018.041,-121.917;Inherit;False;originalColor;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;179;-1036.103,27.02582;Inherit;False;159;uberNoiseTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;237;2948.598,1594.428;Inherit;False;shaderTime;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;235;3662.626,1220.644;Inherit;False;shaderPosition;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;59;2242.011,-2636.393;Inherit;False;Property;_EnableShine;Enable Shine;210;0;Create;True;0;0;0;False;0;False;1;0;0;True;;Toggle;2;Key0;Key1;Reference;56;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;103;2715.721,-2556.586;Inherit;False;_GlitchUV;240;;13133;2addb21417fb5d745a5abfe02cbcd453;0;5;23;FLOAT;0;False;13;FLOAT2;0,0;False;22;SAMPLER2D;;False;3;FLOAT;0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;123;6281.453,1414.289;Inherit;False;fullFade;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;343;-737.796,35.8288;Inherit;False;_UberCustomAlpha;597;;13135;d68af6e3188f53845b23cf6e39df15fe;0;3;1;COLOR;0,0,0,0;False;6;SAMPLER2D;0;False;7;FLOAT2;0,0;False;2;FLOAT;12;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;180;-687.1025,-126.9742;Inherit;False;159;uberNoiseTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;176;2993.115,-2238.526;Inherit;False;159;uberNoiseTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;62;3040.934,-2600.272;Inherit;False;Property;_EnableShine;Enable Shine;227;0;Create;True;0;0;0;False;0;False;1;0;0;True;;Toggle;2;Key0;Key1;Reference;57;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;259;2999.016,-2403.146;Inherit;False;237;shaderTime;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;177;3015.115,-2149.526;Inherit;False;157;spriteTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;258;2972.016,-2323.146;Inherit;False;235;shaderPosition;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.TextureCoordinatesNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;131;3266.866,-2134.612;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;129;3305.944,-1988.403;Inherit;False;123;fullFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleRemainderNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;486;4222.693,-2046.305;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;1,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;381;5159.958,-1852.951;Inherit;False;377;spriteRectMax;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;380;5166.152,-1941.533;Inherit;False;376;spriteRectMin;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;240;-683.451,-246.0232;Inherit;False;237;shaderTime;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;148;-614.0953,-401.0159;Inherit;False;146;finalUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;178;-455.0759,-519.7366;Inherit;False;159;uberNoiseTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;239;-404.7228,-125.1053;Inherit;False;_UberGenerated;571;;13140;52defa3f7cca25740a6a77f065edb382;0;4;10;FLOAT;0;False;8;SAMPLER2D;0;False;7;FLOAT2;0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;242;-432.6598,-617.0601;Inherit;False;235;shaderPosition;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;181;-423.344,-436.9742;Inherit;False;157;spriteTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;183;-37.83691,-91.99512;Inherit;False;182;hologramFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.ScreenPosInputsNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;502;-128,64;Float;False;0;False;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;501;-16,-16;Inherit;False;Constant;_Float2;Float 1;102;0;Create;True;0;0;0;False;0;False;0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;484;3382.41,-2373.518;Inherit;False;_UberTransformUV;485;;13144;894b1de51a5f4c74cbe7828262f1344b;0;5;25;FLOAT;0;False;26;FLOAT2;0,0;False;1;FLOAT2;0,0;False;18;SAMPLER2D;0;False;3;SAMPLER2D;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;130;3651.881,-2106.533;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;487;4449.801,-1972.639;Inherit;False;2;2;0;FLOAT2;0,0;False;1;FLOAT2;1,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;378;5475.994,-2043.135;Inherit;False;Remap2D;-1;;13156;f79f855c0a5c94649b58f3d8127375ae;0;5;13;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;1,1;False;5;FLOAT2;0,0;False;6;FLOAT2;1,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;361;-136.8644,-329.1863;Inherit;False;_UberColor;72;;13157;db48f560e502b78409f7fbe481a93597;0;6;39;FLOAT;0;False;40;FLOAT2;0,0;False;1;FLOAT2;0,0;False;24;SAMPLER2D;0;False;3;COLOR;0,0,0,0;False;5;SAMPLER2D;0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;508;183.7499,-168.0946;Inherit;False;_Hologram;212;;13199;76082a965d84d0e4da33b2cff51b3691;0;5;42;FLOAT;0;False;40;FLOAT;0;False;1;COLOR;1,1,1,1;False;43;FLOAT;0;False;44;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;161;687.0067,-317.0453;Inherit;False;154;glitchPosition;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;162;704.0067,-153.0455;Inherit;False;152;glitchFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;163;668.7452,-235.3598;Inherit;False;159;uberNoiseTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;145;3940.446,-2146.193;Inherit;False;Property;_UberFading;Uber Fading;24;0;Create;True;0;0;0;False;0;False;0;0;0;True;;KeywordEnum;4;NONE;Key1;Key2;Key3;Reference;139;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleRemainderNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;488;4664.06,-2056.074;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;1,1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMaxOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;382;5739.72,-1830.532;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;56;438.7324,-454.4984;Inherit;False;Property;_EnableHologram;Enable Hologram;210;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;104;973.7388,-316.5438;Inherit;False;_Glitch;234;;13202;97a01281f94bcc04fbb9a7c1cd328f08;0;5;34;FLOAT;0;False;31;FLOAT2;0,0;False;33;SAMPLER2D;;False;29;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;164;250.219,-2894.672;Inherit;False;fullDistortionAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;485;4859.071,-2156.215;Inherit;False;Property;_TilingFix;Tiling Fix;11;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMinOpNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;383;5870.72,-1934.532;Inherit;False;2;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;452;6307.805,-1976.09;Inherit;False;450;uvAfterPixelArt;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;442;6299.205,-1884.911;Inherit;False;157;spriteTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;463;6361.842,-2067.504;Inherit;False;460;originalUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;57;1278.486,-397.6114;Inherit;False;Property;_EnableGlitch;Enable Glitch;227;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;247;1456.499,-578.4069;Inherit;False;237;shaderTime;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;171;1364.183,-273.4383;Inherit;False;159;uberNoiseTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;246;1400.383,-493.8317;Inherit;False;235;shaderPosition;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;166;1853.428,-195.4143;Inherit;False;164;fullDistortionAlpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;167;1037.158,-2834.03;Inherit;False;directionalDistortionAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;371;6107.095,-2156.984;Inherit;False;Property;_SpriteSheetFix1;Sprite Sheet Fix;9;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Reference;366;False;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;470;6618.802,-2035.91;Inherit;False;_PixelArtUV_2;-1;;13206;4b65626ba2313ca40a96813b19044794;0;4;21;FLOAT2;0,0;False;17;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;SAMPLER2D;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;473;1656.25,-345.9998;Inherit;False;_UberEffect;245;;13207;93c7a07f758a0814998210619e8ad1cb;0;4;40;FLOAT;0;False;41;FLOAT2;0,0;False;3;COLOR;0,0,0,0;False;37;SAMPLER2D;0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;78;2104.106,-267.8359;Inherit;False;AlphaMultiply;-1;;13263;d24974f7959982d48aab81e9e7692f35;0;2;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;168;2616.17,-223.2014;Inherit;False;167;directionalDistortionAlpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;427;7006.263,-2158.381;Inherit;False;Property;_PixelPerfectUV;Pixel Perfect UV;8;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;77;2492.977,-348.4961;Inherit;False;Property;_EnableFullDistortion;Enable Full Distortion;479;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;76;3014.405,-219.2272;Inherit;False;AlphaMultiply;-1;;13264;d24974f7959982d48aab81e9e7692f35;0;2;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;146;7280.32,-2157.36;Inherit;False;finalUV;-1;True;1;0;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;75;3434.708,-336.5002;Inherit;False;Property;_EnableDirectionalDistortion;Enable Directional Distortion;467;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;All;9;1;COLOR;0,0,0,0;False;0;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;3;COLOR;0,0,0,0;False;4;COLOR;0,0,0,0;False;5;COLOR;0,0,0,0;False;6;COLOR;0,0,0,0;False;7;COLOR;0,0,0,0;False;8;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;249;3627.646,-39.2937;Inherit;False;235;shaderPosition;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;169;3611.012,-138.045;Inherit;False;159;uberNoiseTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;479;3931.587,-267.0083;Inherit;False;157;spriteTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;480;3955.587,-349.0083;Inherit;False;146;finalUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;248;3912.165,-179.9706;Inherit;False;_UberFading;408;;13265;f8f5d1f402d6b694f9c47ef65b4ae91d;0;3;18;FLOAT2;0,0;False;1;COLOR;0,0,0,0;False;17;SAMPLER2D;0;False;1;COLOR;0
Node;AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;193;-400.3044,37.52343;Inherit;False;customVertexAlpha;-1;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;478;4200.417,-206.2577;Inherit;False;_UberColorFinal;43;;13280;6ac57aba23ea6404ba71b6806ea93971;0;3;14;FLOAT2;0,0;False;15;SAMPLER2D;;False;3;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;196;4178.044,-19.14569;Inherit;False;193;customVertexAlpha;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;344;4548.086,-90.46628;Inherit;False;AlphaMultiply;-1;;13291;d24974f7959982d48aab81e9e7692f35;0;2;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;476;3958.333,566.3407;Inherit;False;475;fixedUV;1;0;OBJECT;;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;515;4576,-272;Inherit;False;514;UIVertexColor;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;194;4768,48;Inherit;False;191;originalColor;1;0;OBJECT;;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;516;4768,144;Inherit;False;514;UIVertexColor;1;0;OBJECT;;False;1;FLOAT4;0
Node;AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;200;3977.696,393.365;Inherit;False;Constant;_ZeroVector;Zero Vector;67;0;Create;True;0;0;0;False;0;False;0,0;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;199;4171.966,521.7499;Inherit;False;_Squish;566;;13292;6d6a73cc3433bad4186f7028cad3d98c;0;2;82;FLOAT2;0,0;False;1;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;353;4889.3,-222.093;Inherit;False;ColorMultiply;-1;;13293;1f51da7edd80c06488c56d28bc096dec;0;2;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;124;4962.062,258.7927;Inherit;False;123;fullFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;511;5034.937,70.88478;Inherit;False;ColorMultiply;-1;;13294;1f51da7edd80c06488c56d28bc096dec;0;2;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;198;4453.426,400.9801;Inherit;False;Property;_EnableSquish;Enable Squish;565;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;False;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;342;5134.784,-128.8904;Inherit;False;Property;_VertexTintFirst;Vertex Tint First;6;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;False;True;All;9;1;FLOAT4;0,0,0,0;False;0;FLOAT4;0,0,0,0;False;2;FLOAT4;0,0,0,0;False;3;FLOAT4;0,0,0,0;False;4;FLOAT4;0,0,0,0;False;5;FLOAT4;0,0,0,0;False;6;FLOAT4;0,0,0,0;False;7;FLOAT4;0,0,0,0;False;8;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;251;4509.569,562.3449;Inherit;False;237;shaderTime;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;125;5403.405,74.92608;Inherit;False;3;0;FLOAT4;0,0,0,0;False;1;FLOAT4;0,0,0,0;False;2;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;141;4826.724,532.0556;Inherit;False;123;fullFade;1;0;OBJECT;;False;1;FLOAT;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;424;4759.72,401.6135;Inherit;False;_UberTransformOffset;532;;13295;ee5e9e731457b2342bdb306bdb8d2401;0;2;8;FLOAT;0;False;2;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;143;5663.995,-69.69315;Inherit;False;Property;_UberFading;Uber Fading;24;0;Create;True;0;0;0;False;0;False;0;0;0;True;;KeywordEnum;4;NONE;Key1;Key2;Key3;Reference;139;True;True;All;9;1;FLOAT4;0,0,0,0;False;0;FLOAT4;0,0,0,0;False;2;FLOAT4;0,0,0,0;False;3;FLOAT4;0,0,0,0;False;4;FLOAT4;0,0,0,0;False;5;FLOAT4;0,0,0,0;False;6;FLOAT4;0,0,0,0;False;7;FLOAT4;0,0,0,0;False;8;FLOAT4;0,0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.LerpOp, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;121;5091.355,506.223;Inherit;False;3;0;FLOAT2;0,0;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;340;6010.595,-19.64009;Inherit;False;BakingHandler;12;;13299;f63dfe0dc7c747c43b593d357b168fa0;0;1;7;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;142;5362.269,308.0164;Inherit;False;Property;_UberFading;Uber Fading;24;0;Create;True;0;0;0;False;0;False;0;0;0;True;;KeywordEnum;4;NONE;Key1;Key2;Key3;Reference;139;True;True;All;9;1;FLOAT2;0,0;False;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT2;0,0;False;6;FLOAT2;0,0;False;7;FLOAT2;0,0;False;8;FLOAT2;0,0;False;1;FLOAT2;0
Node;AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;187;489.4036,-2756.916;Inherit;False;157;spriteTexture;1;0;OBJECT;;False;1;SAMPLER2D;0
Node;AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null;489;6391.947,126.1744;Float;False;True;-1;2;SpriteShadersUltimate.SSUShaderGUI;0;3;Sprite Shaders Ultimate/GUI SSU;5056123faa0c79b47ab6ad7e8bf059a4;True;Default;0;0;Default;2;True;True;3;1;False;;10;False;;3;1;False;;10;False;;False;False;False;False;False;False;False;False;False;False;False;False;True;2;False;;True;True;True;True;True;True;0;True;_ColorMask;False;False;False;False;False;False;True;True;True;0;True;_Stencil;255;True;_StencilReadMask;255;True;_StencilWriteMask;0;True;_StencilComp;0;True;_StencilOp;0;False;;0;False;;0;False;;0;False;;0;False;;0;False;;False;True;2;False;;True;0;True;unity_GUIZTestMode;False;True;5;Queue=Transparent=Queue=0;IgnoreProjector=True;RenderType=Transparent=RenderType;PreviewType=Plane;CanUseSpriteAtlas=True;False;False;0;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;3;False;0;;0;0;Standard;0;0;1;True;False;;False;0
WireConnection;483;1;363;0
WireConnection;483;12;411;0
WireConnection;482;1;483;0
WireConnection;482;12;411;0
WireConnection;460;0;482;0
WireConnection;469;1;460;0
WireConnection;469;2;411;0
WireConnection;374;0;373;1
WireConnection;374;1;373;2
WireConnection;375;0;373;3
WireConnection;375;1;373;4
WireConnection;105;0;30;0
WireConnection;449;1;460;0
WireConnection;449;0;469;0
WireConnection;377;0;375;0
WireConnection;376;0;374;0
WireConnection;157;0;105;0
WireConnection;450;0;449;0
WireConnection;370;13;450;0
WireConnection;370;5;376;0
WireConnection;370;6;377;0
WireConnection;438;0;157;0
WireConnection;366;1;450;0
WireConnection;366;0;370;0
WireConnection;99;0;101;0
WireConnection;432;0;438;0
WireConnection;475;0;366;0
WireConnection;159;0;99;0
WireConnection;434;0;435;0
WireConnection;434;1;432;0
WireConnection;365;9;477;0
WireConnection;365;3;475;0
WireConnection;513;3;509;0
WireConnection;433;0;434;0
WireConnection;84;0;365;0
WireConnection;79;195;253;0
WireConnection;79;160;84;0
WireConnection;79;194;184;0
WireConnection;471;1;147;0
WireConnection;471;2;189;0
WireConnection;514;0;513;0
WireConnection;436;0;433;0
WireConnection;436;1;432;0
WireConnection;83;1;84;0
WireConnection;83;0;79;174
WireConnection;81;182;254;0
WireConnection;81;160;83;0
WireConnection;81;181;188;0
WireConnection;102;19;244;0
WireConnection;102;18;243;0
WireConnection;102;16;160;0
WireConnection;494;1;471;0
WireConnection;356;1;494;0
WireConnection;356;2;514;0
WireConnection;437;1;435;0
WireConnection;437;0;436;0
WireConnection;82;1;83;0
WireConnection;82;0;81;174
WireConnection;182;0;53;0
WireConnection;152;0;102;15
WireConnection;154;0;102;0
WireConnection;292;1;122;0
WireConnection;292;2;204;0
WireConnection;223;21;261;0
WireConnection;223;1;122;0
WireConnection;223;16;219;0
WireConnection;223;18;208;0
WireConnection;223;20;210;0
WireConnection;231;27;261;0
WireConnection;231;22;122;0
WireConnection;231;18;228;0
WireConnection;231;25;208;0
WireConnection;231;23;229;0
WireConnection;231;21;210;0
WireConnection;231;26;230;0
WireConnection;354;1;494;0
WireConnection;354;0;356;0
WireConnection;439;61;437;0
WireConnection;439;3;441;0
WireConnection;38;0;82;0
WireConnection;507;77;255;0
WireConnection;507;55;182;0
WireConnection;507;76;173;0
WireConnection;507;37;38;0
WireConnection;507;39;174;0
WireConnection;507;81;499;0
WireConnection;507;79;500;2
WireConnection;139;1;122;0
WireConnection;139;0;122;0
WireConnection;139;2;292;0
WireConnection;139;3;223;0
WireConnection;139;4;231;0
WireConnection;191;0;354;0
WireConnection;237;0;341;0
WireConnection;235;0;439;0
WireConnection;59;1;38;0
WireConnection;59;0;507;0
WireConnection;103;23;256;0
WireConnection;103;13;155;0
WireConnection;103;22;175;0
WireConnection;103;3;153;0
WireConnection;103;1;59;0
WireConnection;123;0;139;0
WireConnection;343;1;191;0
WireConnection;343;6;179;0
WireConnection;343;7;149;0
WireConnection;62;1;59;0
WireConnection;62;0;103;0
WireConnection;486;0;145;0
WireConnection;239;10;240;0
WireConnection;239;8;180;0
WireConnection;239;7;148;0
WireConnection;239;1;343;0
WireConnection;484;25;259;0
WireConnection;484;26;258;0
WireConnection;484;1;62;0
WireConnection;484;18;176;0
WireConnection;484;3;177;0
WireConnection;130;0;131;0
WireConnection;130;1;484;0
WireConnection;130;2;129;0
WireConnection;487;0;486;0
WireConnection;378;13;485;0
WireConnection;378;2;380;0
WireConnection;378;3;381;0
WireConnection;361;39;240;0
WireConnection;361;40;242;0
WireConnection;361;1;148;0
WireConnection;361;24;178;0
WireConnection;361;3;239;0
WireConnection;361;5;181;0
WireConnection;508;42;240;0
WireConnection;508;40;183;0
WireConnection;508;1;361;0
WireConnection;508;43;502;2
WireConnection;508;44;501;0
WireConnection;145;1;484;0
WireConnection;145;0;130;0
WireConnection;145;2;130;0
WireConnection;145;3;130;0
WireConnection;145;4;130;0
WireConnection;488;0;487;0
WireConnection;382;0;378;0
WireConnection;382;1;380;0
WireConnection;56;1;361;0
WireConnection;56;0;508;0
WireConnection;104;34;244;0
WireConnection;104;31;161;0
WireConnection;104;33;163;0
WireConnection;104;29;162;0
WireConnection;104;1;56;0
WireConnection;164;0;79;0
WireConnection;485;1;145;0
WireConnection;485;0;488;0
WireConnection;383;0;382;0
WireConnection;383;1;381;0
WireConnection;57;1;56;0
WireConnection;57;0;104;0
WireConnection;167;0;81;0
WireConnection;371;1;485;0
WireConnection;371;0;383;0
WireConnection;470;21;463;0
WireConnection;470;17;452;0
WireConnection;470;1;371;0
WireConnection;470;2;442;0
WireConnection;473;40;247;0
WireConnection;473;41;246;0
WireConnection;473;3;57;0
WireConnection;473;37;171;0
WireConnection;78;1;473;0
WireConnection;78;2;166;0
WireConnection;427;1;371;0
WireConnection;427;0;470;0
WireConnection;77;1;473;0
WireConnection;77;0;78;0
WireConnection;76;1;77;0
WireConnection;76;2;168;0
WireConnection;146;0;427;0
WireConnection;75;1;77;0
WireConnection;75;0;76;0
WireConnection;248;18;249;0
WireConnection;248;1;75;0
WireConnection;248;17;169;0
WireConnection;193;0;343;12
WireConnection;478;14;480;0
WireConnection;478;15;479;0
WireConnection;478;3;248;0
WireConnection;344;1;478;0
WireConnection;344;2;196;0
WireConnection;199;82;200;0
WireConnection;199;1;476;0
WireConnection;353;1;344;0
WireConnection;353;2;515;0
WireConnection;511;1;194;0
WireConnection;511;2;516;0
WireConnection;198;1;200;0
WireConnection;198;0;199;0
WireConnection;342;1;353;0
WireConnection;342;0;344;0
WireConnection;125;0;511;0
WireConnection;125;1;342;0
WireConnection;125;2;124;0
WireConnection;424;8;251;0
WireConnection;424;2;198;0
WireConnection;143;1;342;0
WireConnection;143;0;125;0
WireConnection;143;2;125;0
WireConnection;143;3;125;0
WireConnection;143;4;125;0
WireConnection;121;1;424;0
WireConnection;121;2;141;0
WireConnection;340;7;143;0
WireConnection;142;1;424;0
WireConnection;142;0;121;0
WireConnection;142;2;121;0
WireConnection;142;3;121;0
WireConnection;142;4;121;0
WireConnection;489;0;340;0
WireConnection;489;1;142;0
ASEEND*/
//CHKSM=E6E81690425DA4FCBF16411B79B4AACED90D357C