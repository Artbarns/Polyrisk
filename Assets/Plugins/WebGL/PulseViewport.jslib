mergeInto(LibraryManager.library, {
  PolyRiskSafeInset: function (edge) {
    if (!Module.polyRiskSafeInsetElement) {
      var element = document.createElement('div');
      element.style.cssText = 'position:fixed;visibility:hidden;pointer-events:none;width:0;height:0;padding-left:env(safe-area-inset-left,0px);padding-bottom:env(safe-area-inset-bottom,0px);padding-right:env(safe-area-inset-right,0px);padding-top:env(safe-area-inset-top,0px)';
      document.body.appendChild(element);
      Module.polyRiskSafeInsetElement = element;
    }
    var style = window.getComputedStyle(Module.polyRiskSafeInsetElement);
    return parseFloat([style.paddingLeft, style.paddingBottom, style.paddingRight, style.paddingTop][edge]) || 0;
  },
  PolyRiskCanvasCssWidth: function () {
    return Module.canvas ? Module.canvas.getBoundingClientRect().width : 0;
  }
});
