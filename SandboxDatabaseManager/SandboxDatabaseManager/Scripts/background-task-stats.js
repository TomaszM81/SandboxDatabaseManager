
$(function () {

    var ticker = $.connection.backgroundTasksHub; // the generated client-side hub proxy

    function init() {
        ticker.server.getBackgroundStats().done(function (statsData) {



            $.each(statsData, function () {
                $contersTable = $('#' + this.Status);
                $contersTable.text(this.Count);


                if (this.Status == "Running")
                {
                    if(this.Count == 0)
                    {
                        $contersTable.removeClass("stats_running");
                    }else{
                        $contersTable.addClass("stats_running");
                    }

                } else if (this.Status == "Succeeded"){
                    if (this.Count == 0) {
                        $contersTable.removeClass("stats_success");
                    } else {
                        $contersTable.addClass("stats_success");
                    }

                } else {
                    if (this.Count == 0) {
                        $contersTable.removeClass("stats_failed");
                    } else {
                        $contersTable.addClass("stats_failed");
                    }

                }


            });





        });
    }

    // Add a client-side hub method that the server will call
    ticker.client.updateBackgroundTaskStats = function (statsData) {

        $.each(statsData, function () {
            $contersTable = $('#' + this.Status);
            $contersTable.text(this.Count);


            if (this.Status == "Running") {
                if (this.Count == 0) {
                    $contersTable.removeClass("stats_running");
                } else {
                    $contersTable.addClass("stats_running");
                }

            } else if (this.Status == "Succeeded") {
                if (this.Count == 0) {
                    $contersTable.removeClass("stats_success");
                } else {
                    $contersTable.addClass("stats_success");
                }

            } else {
                if (this.Count == 0) {
                    $contersTable.removeClass("stats_failed");
                } else {
                    $contersTable.addClass("stats_failed");
                }

            }


        });
    }

    // Start the connection
    $.connection.hub.start().done(init);

});