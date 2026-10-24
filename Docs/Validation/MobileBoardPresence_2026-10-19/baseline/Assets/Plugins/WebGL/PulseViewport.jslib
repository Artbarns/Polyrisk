mergeInto(LibraryManager.library, {
  PolyRiskCanvasCssWidth: function () {
    return Module.canvas ? Module.canvas.getBoundingClientRect().width : 0;
  }
});
