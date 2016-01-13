$("#menu-toggle").click(function (e) {
    e.preventDefault();
    

    $("#wrapper").addClass("slow_toggle");
    $("#sidebar-wrapper").addClass("slow_toggle");
    $("#glyphicon_monitor").removeClass("glyphicon-menu-right");
    $("#glyphicon_monitor").removeClass("glyphicon-menu-left");

    if($("#wrapper").hasClass("toggled"))
    {

        $("#glyphicon_monitor").addClass("glyphicon-menu-left");
        $("#wrapper").removeClass("toggled");
        $.cookie("sidebar_visible", "false", { path: '/' });

        if (typeof graph !== 'undefined') {
            interval = setInterval(function () { graph.resize(); }, 10);
            setTimeout(function () { clearInterval(interval); }, 550);
        }

    }else
    {
        $("#wrapper").addClass("toggled");
        $("#glyphicon_monitor").addClass("glyphicon-menu-right");
        $.cookie("sidebar_visible", "true", { path: '/' });

        if (typeof graph !== 'undefined') {
            interval = setInterval(function () { graph.resize();}, 10);
            setTimeout(function () { clearInterval(interval); }, 550);
        }

    }


});

$(document).ready(function () {
    var last = $.cookie("sidebar_visible");
    if (last === "true") {
        $("#wrapper").addClass("toggled");
        $("#glyphicon_monitor").removeClass("glyphicon-menu-left");
        $("#glyphicon_monitor").addClass("glyphicon-menu-right");
    }
});

