
$(function () {

    var ticker = $.connection.monitoringHub; // the generated client-side hub proxy

    function init() {
        ticker.server.getCounterStats().done(function (countersData) {
            $.each(countersData, function () {
                $contersTable = $('#' + this.ServerName);
                $contersTable.empty();
                $contersTable.append('<tr><th colspan="3">' + this.ServerName + '</th></tr>');

                var server = this.ServerName;
                $.each(this.ListOfCounterValues, function () {
                    var uri = basepath + 'Stats/GetStats?serverName=' + server + '&counterName=' + this.CounterFriendlyName + '&chartYAxisSufix=' + this.ChartYAxisSufix;

                    if(this.IsWarning) {
                        $contersTable.append('<tr><td><a href="' + encodeURI(uri) + '">' + this.CounterFriendlyName + ':</a></td><td><font color="#FF0000">' + this.CounterFormattedValue + ' </font></td></tr>');
                    }else{
                        $contersTable.append('<tr><td><a href="' + encodeURI(uri) + '">' + this.CounterFriendlyName + ':</a></td><td>' + this.CounterFormattedValue + '</td></tr>');
                    }

                    
                });

            });

           
         


        });
    }

    // Add a client-side hub method that the server will call
    ticker.on('updatePerformanceCounterStats', function (countersData) {

        $.each(countersData, function () {
            $contersTable = $('#' + this.ServerName);
            $contersTable.empty();
            $contersTable.append('<tr><th colspan="3">' + this.ServerName + '</th></tr>');

            var server = this.ServerName;
            $.each(this.ListOfCounterValues, function () {
               var uri = basepath + 'Stats/GetStats?serverName=' + server + '&counterName=' +this.CounterFriendlyName + '&chartYAxisSufix=' +this.ChartYAxisSufix;

                if (this.IsWarning) {
                    $contersTable.append('<tr><td><a href="' + encodeURI(uri) + '">' + this.CounterFriendlyName + ':</a></td><td><font color="#FF0000">' + this.CounterFormattedValue + ' </font></td></tr>');
                } else {
                    $contersTable.append('<tr><td><a href="' + encodeURI(uri) + '">' + this.CounterFriendlyName + ':</a></td><td>' + this.CounterFormattedValue + '</td></tr>');
                }
            });

        });
    });

    // Start the connection
    $.connection.hub.start().done(init);

});