(function ($) {
    $.fn.remoteProcessing = function (options) {
        return this.each(function () {
            var $this = $(this);

            var opt = $.extend({}, { "apiurl": "/webapi/" }, options, $this.data());

            var BillingCategory = {
                Tool: 1,
                Room: 2,
                Store: 4
            };

            function getPeriod() {
                var m = moment($(".period-picker", $this).datepicker('getDate'));
                return m.format('YYYY-MM-01');
            }

            function getRemoteProcessingUrl(period) {
                var sd = period;
                var ed = moment(period).add(1, 'months').format('YYYY-MM-01');
                return opt.apiurl + 'data/client/remote/active/range?sd=' + sd + '&ed=' + ed;
            }

            function getStaffUrl(period) {
                var sd = period;
                var ed = moment(period).add(1, 'months').format('YYYY-MM-01');
                return opt.apiurl + 'data/client/active/range?sd=' + sd + '&ed=' + ed + '&privs=2';
            }

            function getRemoteUserUrl(period) {
                var sd = period;
                var ed = moment(period).add(1, 'months').format('YYYY-MM-01');
                return opt.apiurl + 'data/client/active/range?sd=' + sd + '&ed=' + ed + '&privs=128';
            }

            function getRemoteAcctUrl(clientId, period) {
                var sd = period;
                var ed = moment(period).add(1, 'months').format('YYYY-MM-01');
                return opt.apiurl + 'data/client/' + clientId + '/accounts/active/range?sd=' + sd + '&ed=' + ed;
            }

            function getDeleteClientRemoteUrl(clientRemoteId) {
                return opt.apiurl + 'data/client/remote/' + clientRemoteId;
            }

            function getUpdateBillingUrl() {
                return opt.apiurl + 'billing/process/remote-processing-update';
            }

            function getRegularExceptionUrl(period) {
                return opt.apiurl + 'billing/report/regular-exception?period=' + period;
            }

            function getRemoteProcessingReservationUrl(period) {
                var sd = period;
                var ed = moment(period).add(1, 'months').format('YYYY-MM-01');
                return opt.apiurl + 'scheduler/reservation?sd=' + sd + "&ed=" + ed + "&activityId=22";
            }

            function getAddClientRemoteUrl(period) {
                return opt.apiurl + 'data/client/remote?period=' + moment(period).format('YYYY-MM-01');
            }

            function fillStaffSelect() {
                var select = $(".staff-select", $this);

                select.hide();

                $(".staff-loading", $this).show();

                $.ajax({
                    'url': getStaffUrl(getPeriod())
                }).done(function (data) {

                    if (data.length === 0) {
                        select.html($("<option/>").val(0).html("No active staff this period")).prop('disabled', true);
                        return;
                    }

                    select.html($.map(data, function (val, i) {
                        return $('<option/>', { "value": val.ClientID }).html(val.LName + ', ' + val.FName);
                    }));
                }).always(function () {
                    select.show();
                    $(".staff-loading", $this).hide();
                });
            }

            function fillRemoteUserSelect(callback) {
                var select = $(".remote-user-select", $this);

                select.hide().prop('disabled', false);
                $(".remote-user-loading", $this).show();

                $.ajax({
                    'url': getRemoteUserUrl(getPeriod())
                }).done(function (data) {

                    if (data.length === 0) {
                        select.html($("<option/>").val(0).html("No active remote users this period")).prop('disabled', true);
                        return;
                    }

                    select.html($.map(data, function (val, i) {
                        return $('<option/>', { "value": val.ClientID }).html(val.LName + ', ' + val.FName);
                    }));
                }).always(function () {
                    select.show();
                    $(".remote-user-loading", $this).hide();

                    if (typeof callback === 'function')
                        callback();
                });
            }

            function fillRemoteAcctSelect() {
                var select = $(".remote-acct-select", $this);

                select.hide().prop('disabled', false);
                $(".remote-acct-loading", $this).show();

                var clientId = $(".remote-user-select", $this).val();

                $.ajax({
                    'url': getRemoteAcctUrl(clientId, getPeriod())
                }).done(function (data) {

                    if (data.length === 0) {
                        select.html($("<option/>").val(0).html("No active accounts this period")).prop('disabled', true);
                        return;
                    }

                    select.html($.map(data, function (val, i) {
                        return $('<option/>', { "value": val.AccountID }).html(val.AccountName);
                    }));
                }).always(function () {
                    select.show();
                    $(".remote-acct-loading", $this).hide();
                });
            }

            function diplayAlert(type, msg, dismissible) {
                var alert = $("<div/>", { "class": "alert alert-" + type, "role": "alert" }).html(msg);
                if (dismissible) {
                    alert
                        .addClass('alert-dismissible')
                        .prepend(
                            $("<button/>", { "type": "button", "class": "close", "data-dismiss": "alert", "aria-label": "Close" }).html(
                                $("<span/>", { "aria-hidden": true }).html("&times;")
                            )
                        );
                }
                $(".message").html(alert);
            }

            function displayName(item) {
                if (!item) return '';

                if (!item.LName && !item.FName)
                    return '';

                if (!item.LName && item.FName)
                    return item.FName;

                if (item.LName && !item.FName)
                    return item.LName;

                return item.LName + ', ' + item.FName;
            }

            function getInvitee(row) {
                if (row.Invitees && row.Invitees.length > 0) {
                    return row.Invitees[0];
                } else {
                    return {
                        ReservationID: 0,
                        ClientID: 0,
                        LName: "",
                        FName: ""
                    };
                }
            }

            function getBillingCategory(row) {
                if (row.BillingCategory === BillingCategory.Tool)
                    return 'Tool';
                else if (row.BillingCategory === BillingCategory.Room)
                    return 'Room';
                else
                    return 'unknown [' + row.BillingCategory + ']';
            }

            function handleClientRemotesDeleteColumn(row, type, set, meta) {
                return '<a href="#" class="delete-link" data-client-remote-id="' + row.ClientRemoteID + '" data-client-id="' + row.ClientID + '" data-account-id="' + row.AccountID + '">delete</a>';
            }

            function handleClientRemotesRefreshColumn(row, type, set, meta) {
                return '<a href="#" class="refresh-link" data-client-remote-id="' + row.ClientRemoteID + '"  data-client-id="' + row.ClientID + '" data-account-id="' + row.AccountID + '">refresh</a>';
            }

            function handleRegularExceptionsAddColumn(row, type, set, meta) {
                if (row.BillingCategory !== BillingCategory.Tool) return '';
                return '<a href="#" class="add-link" data-client-id="' + row.ClientID + '" data-remote-client-id="' + row.InviteeClientID + '" data-account-id="' + row.AccountID + '">add</a>';
            }

            function toggleRefreshOn(root) {
                var div = $(".refresh", root);
                $("a", div).hide();
                $(".loading", div).show();
                $(".refresh-link, .delete-link, .add-link", root).hide();
            }

            function toggleRefreshOff(root) {
                var div = $(".refresh", root);
                $("a", div).show();
                $(".loading", div).hide();
                $(".refresh-link, .delete-link, .add-link", root).show();
            }

            function refreshClientRemotes(period) {
                var def = $.Deferred();

                toggleRefreshOn($(".client-remotes", $this));

                clientRemotes.api().ajax.url(getRemoteProcessingUrl(period)).load(function () {
                    toggleRefreshOff($(".client-remotes", $this));
                    def.resolve();
                });

                return def.promise();
            }

            function refreshRegularExceptions(period) {
                var def = $.Deferred();

                toggleRefreshOn($(".regular-exceptions", $this));

                regularExceptions.api().ajax.url(getRegularExceptionUrl(period)).load(function () {
                    toggleRefreshOff($(".regular-exceptions", $this));
                    def.resolve();
                });

                return def.promise();
            }

            function refreshRemprocReservations(period) {
                var def = $.Deferred();

                toggleRefreshOn($(".remproc-reservations", $this));

                remprocReservations.api().ajax.url(getRemoteProcessingReservationUrl(period)).load(function () {
                    toggleRefreshOff($(".remproc-reservations", $this));
                    def.resolve();
                });

                return def.promise();
            }

            function updateBilling(args) {
                var def = $.Deferred();

                toggleRefreshOn($(".client-remotes", $this));
                toggleRefreshOn($(".regular-exceptions", $this));

                $.ajax({
                    'url': getUpdateBillingUrl(),
                    'type': 'POST',
                    'data': args
                }).always(function () {
                    refreshRegularExceptions(args.Period).always(function () {
                        toggleRefreshOff($(".client-remotes", $this));
                        toggleRefreshOff($(".regular-exceptions", $this));
                        def.resolve();
                    });
                });

                return def.promise();
            }

            function createClientRemote(period, args) {
                var def = $.Deferred();

                toggleRefreshOn($(".client-remotes", $this));
                toggleRefreshOn($(".regular-exceptions", $this));

                $.ajax({
                    'url': getAddClientRemoteUrl(period),
                    'type': 'POST',
                    'data': args
                }).always(function () {
                    refreshClientRemotes(period).always(function () {
                        updateBilling({ 'ClientID': args.Client.ClientID, 'AccountID': args.Account.AccountID, 'Period': period }).always(function () {
                            toggleRefreshOff($(".client-remotes", $this));
                            toggleRefreshOff($(".regular-exceptions", $this));
                            def.resolve();
                        });
                    });
                });

                return def.promise();
            }

            function deleteClientRemote(period, args) {
                var def = $.Deferred();

                toggleRefreshOn($(".client-remotes", $this));
                toggleRefreshOn($(".regular-exceptions", $this));

                $.ajax({
                    'url': getDeleteClientRemoteUrl(args.ClientRemoteID),
                    'type': 'DELETE'
                }).always(function () {
                    refreshClientRemotes(period).always(function () {
                        updateBilling({ 'ClientID': args.Client.ClientID, 'AccountID': args.Account.AccountID, 'Period': period }).always(function () {
                            toggleRefreshOff($(".client-remotes", $this));
                            toggleRefreshOff($(".regular-exceptions", $this));
                            def.resolve();
                        });
                    });
                });

                return def.promise();
            }

            var clientRemotes = null;
            var regularExceptions = null;
            var remprocReservations = null;

            var pp = $(".period-picker").datepicker({
                autoclose: true,
                viewMode: "months",
                minViewMode: "months",
                format: "M yyyy"
            }).on("changeDate", function (e) {
                var period = getPeriod();

                $this.trigger('refresh-client-remotes', period);
                $this.trigger('refresh-regular-exceptions', period);
                $this.trigger('refresh-remproc-reservations', period);

                fillStaffSelect();

                fillRemoteUserSelect(function () {
                    fillRemoteAcctSelect();
                });
            });

            clientRemotes = $(".remote-processing-table", $this).dataTable({
                stateSave: true,
                ajax: {
                    url: getRemoteProcessingUrl(getPeriod()),
                    type: 'GET',
                    dataSrc: ''
                },
                columns: [
                    { data: handleClientRemotesDeleteColumn, className: 'text-center', orderable: false },
                    { data: function (row, type, set, meta) { return row.DisplayName; } },
                    { data: function (row, type, set, meta) { return row.AccountName; } },
                    { data: function (row, type, set, meta) { return row.RemoteDisplayName; } },
                    { data: handleClientRemotesRefreshColumn, className: 'text-center', orderable: false }
                ],
                order: [[1, 'asc']],
                lengthMenu: [[10, 25, 50, -1], [10, 25, 50, "All"]]
            });

            regularExceptions = $(".regular-exception-table", $this).dataTable({
                stateSave: false,
                ajax: {
                    url: getRegularExceptionUrl(getPeriod()),
                    type: 'GET',
                    dataSrc: ''
                },
                columns: [
                    { data: handleRegularExceptionsAddColumn, className: 'text-center', orderable: false },
                    { data: function (row, type, set, meta) { return getBillingCategory(row); }, width: '30px' },
                    { data: function (row, type, set, meta) { return displayName(row); }, width: '150px' },
                    { data: function (row, type, set, meta) { return displayName({ "LName": row.InviteeLName, "FName": row.InviteeFName }); }, width: '150px' },
                    { data: function (row, type, set, meta) { return row.ReservationID === 0 ? '' : row.ReservationID; }, width: '80px' },
                    { data: 'ResourceName', width: '250px' },
                    { data: 'AccountName' }
                ],
                order: [[1, 'desc'], [2, 'asc']],
                lengthMenu: [[10, 25, 50, -1], [10, 25, 50, "All"]]
            });

            remprocReservations = $(".remproc-reservation-table", $this).dataTable({
                stateSave: false,
                ajax: {
                    url: getRemoteProcessingReservationUrl(getPeriod()),
                    type: 'GET',
                    dataSrc: ''
                },
                columns: [
                    { data: 'ReservationID', width: '80px' },
                    { data: 'ResourceName', width: '200px' },
                    { data: function (row, type, set, meta) { return displayName(row); }, width: '120px' },
                    { data: function (row, type, set, meta) { return displayName(getInvitee(row)); }, width: '120px' },
                    { data: 'AccountName' },
                    { data: function (row, type, set, meta) { return moment(row.BeginDateTime).format('M/D/YYYY[<br>]h:mm:ss A'); }, width: '100px' },
                    { data: function (row, type, set, meta) { return moment(row.EndDateTime).format('M/D/YYYY[<br>]h:mm:ss A'); }, width: '100px' },
                    { data: 'IsActive', width: '50px' },
                    { data: 'IsStarted', width: '50px' },
                    { data: 'ChargeMultiplier', width: '50px' }
                ]
            });

            // event handlers for links, buttons, and selects
            $this.on('change', '.remote-user-select', function (e) {
                fillRemoteAcctSelect();
            }).on('click', '.create-link', function (e) {
                e.preventDefault();

                var clientId = $(".staff-select", $this).val();
                var remoteClientId = $(".remote-user-select", $this).val();
                var accountId = $(".remote-acct-select", $this).val();
                var period = getPeriod();

                if (clientId === 0) {
                    diplayAlert('danger', 'Please select a staff member.', true);
                    return;
                }

                if (remoteClientId === 0) {
                    diplayAlert('danger', 'Please select a remote user.', true);
                    return;
                }

                if (accountId === 0) {
                    diplayAlert('danger', 'Please select a remote account.', true);
                    return;
                }

                $this.trigger('create-client-remote', [period, {
                    'Client': { 'ClientID': clientId },
                    'RemoteClient': { 'ClientID': remoteClientId },
                    'Account': { 'AccountID': accountId }
                }]);

            }).on('click', '.delete-link', function (e) {
                e.preventDefault();

                var clientRemoteId = $(this).data("client-remote-id");
                var clientId = $(this).data("client-id");
                var accountId = $(this).data("account-id");
                var period = getPeriod();

                $this.trigger('delete-client-remote', [period, {
                    'ClientRemoteID': clientRemoteId,
                    'ClientID': clientId,
                    'AccountID': accountId
                }]);
            }).on('click', '.refresh-link', function (e) {
                e.preventDefault();

                var clientId = $(this).data('client-id');
                var accountId = $(this).data('account-id');

                $this.trigger('update-billing', {
                    'ClientID': clientId,
                    'AccountID': accountId,
                    'Period': getPeriod()
                });
            }).on('click', '.add-link', function (e) {
                e.preventDefault();

                var clientId = $(this).data('client-id');
                var remoteClientId = $(this).data('remote-client-id');
                var accountId = $(this).data('account-id');
                var period = getPeriod();

                $this.trigger('create-client-remote', [period, {
                    'Client': { 'ClientID': clientId },
                    'RemoteClient': { 'ClientID': remoteClientId },
                    'Account': { 'AccountID': accountId }
                }]);
            }).on('click', '.refresh-client-remotes > a', function (e) {
                e.preventDefault();
                $this.trigger('refresh-client-remotes', getPeriod());
            }).on('click', '.refresh-regular-exceptions > a', function (e) {
                e.preventDefault();
                $this.trigger('refresh-regular-exceptions', getPeriod());
            }).on('click', '.refresh-remproc-reservations > a', function (e) {
                e.preventDefault();
                $this.trigger('refresh-remproc-reservations', getPeriod());
            });

            // custom events
            $this.on('create-client-remote', function (e, period, args) {
                createClientRemote(period, args);
            }).on('delete-client-remote', function (e, period, args) {
                deleteClientRemote(period, args);
            }).on('update-billing', function (e, args) {
                updateBilling(args);
            }).on('refresh-client-remotes', function (e, period) {
                refreshClientRemotes(period);
            }).on('refresh-regular-exceptions', function (e, period) {
                refreshRegularExceptions(period);
            }).on('refresh-remproc-reservations', function (e, period) {
                refreshRemprocReservations(period);
            });

            fillStaffSelect();

            fillRemoteUserSelect(function () {
                fillRemoteAcctSelect();
            });
        });
    };
}(jQuery));