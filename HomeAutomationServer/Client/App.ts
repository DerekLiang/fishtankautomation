///<reference path="../Scripts/typings/jquery/jquery.d.ts"/>
///<reference path="../Scripts/typings/angularjs/angular.d.ts"/>
///<reference path="../Scripts/typings/angular-ui/angular-ui-router.d.ts"/>
///<reference path="../Scripts/typings/lodash/lodash.d.ts"/>
///<reference path="../Scripts/typings/moment/moment.d.ts"/>

//get the coord@ https://gist.github.com/DerekLiang/621c75c554b12d81842d

interface ISensorInfo {
    id: number;
    sensorType : SensorTypeEnum;
    coordsXY: number[];
    hasError : boolean;
}

interface IAppModel extends ng.IScope  {
    coords : any;
    vm: {
        sensorGetLogClicked: (index: ISensorInfo) => void;
        onClickScheduleButton: () => void;
        updateSensorInfo : () => void;
        sensorInfo: ISensorInfo[];

        loading : boolean;
        isReadyOnly: boolean;

        pumpDelayToInSeconds: number;
        pumpIsOn: boolean;
        lastUpdatedOnDateTime: Date;

        pumpDelayToInSecondsStr: string;
        lastUpdatedOnDateTimeStr : string;

        //api key
        toggleAPIKeyDisplay: () => void;
        showAPIKey : boolean;
    };
}

interface ISensorLog {
    created: string;
    createdStr: string;
    isConnected: boolean;
    fromNow: string;
    forHowLongStr: string;
}

interface ISensorLogScope {
    vm: {
        ok: () => void;
        cancel: () => void;
        logs: ISensorLog[];
        sensorId: number;
        sensorType : SensorTypeEnum;
    }
}

class SensorLogModalController  {
    static $inject = ['$scope', '$modalInstance', 'sensorLogs', 'sensorId', 'sensorType'];

    constructor($scope: ISensorLogScope, $modalInstance, sensorLogs: ng.IHttpPromiseCallbackArg<IAjaxCallResult<ISensorLog[]>>, sensorId: number, sensorType : SensorTypeEnum) {
        $scope.vm = <any>{};
        
        $scope.vm.logs = sensorLogs.data.data;
        $scope.vm.sensorId = sensorId;
        $scope.vm.sensorType = sensorType;

        function millisecondsToStr(milliseconds) {
            // TIP: to find current time in milliseconds, use:
            // var  current_time_milliseconds = new Date().getTime();

            function numberEnding(number) {
                return (number > 1) ? 's' : '';
            }

            var temp = Math.floor(milliseconds / 1000);
            var years = Math.floor(temp / 31536000);
            if (years) {
                return years + ' year' + numberEnding(years);
            }
            //TODO: Months! Maybe weeks? 
            var days = Math.floor((temp %= 31536000) / 86400);
            if (days) {
                return days + ' day' + numberEnding(days);
            }
            var hours = Math.floor((temp %= 86400) / 3600);
            if (hours) {
                return hours + ' hour' + numberEnding(hours);
            }
            var minutes = Math.floor((temp %= 3600) / 60);
            if (minutes) {
                return minutes + ' minute' + numberEnding(minutes);
            }
            var seconds = temp % 60;
            if (seconds) {
                return seconds + ' second' + numberEnding(seconds);
            }
            return 'less than a second'; //'just now' //or other string you like;
        }

        _.each($scope.vm.logs, function (l: ISensorLog, index: number) {
            var created = moment.utc(l.created);
            l.fromNow = created.fromNow();
            var localTime = created.toDate();
            l.createdStr = moment(localTime).format('YYYY-MM-DD HH:mm:ss');

            if (index == 0) l.forHowLongStr = "N/A";
            else {
                var diff = moment.utc($scope.vm.logs[index - 1].created).toDate().getTime() - localTime.getTime();
                console.log('diff', diff, moment.utc($scope.vm.logs[index - 1].created).toDate().getTime(), localTime.getTime());
                l.forHowLongStr = millisecondsToStr(diff);
            }
        });

        $scope.vm.ok = function () {
            $modalInstance.close();
        };

        $scope.vm.cancel = function () {
            $modalInstance.dismiss('cancel');
        };
    }
}

interface IScheduleModal extends ng.IScope {
    vm: {
        ok : () => void;
        cancel: () => void;
        onCLickTimeSlotReserseAll: () => void;
        onClickTimeSlot: (ITimeSlot) => void;
        onClickTimeSlotHour: (timeslots : ITimeSlot[]) => void;
        onCLickTimeSlotWeekDay: (day : number) => void;
        scheduleTimeSlot: ITimeSlot[][];
        header: string[];
        isReadOnly: boolean;


        //for showing message;
        isSaving:boolean;
        hasError:boolean;
    };
}

interface  ITimeSlot {
    DayOfWeek: number;
    Hour: number;
    IsOn: boolean;
}

enum SensorTypeEnum {
    WetSensor = 1,
    DrySensor = 2,
    PumpSwitch = 3,
}

interface ISensorInfoDto {
    Id: number;
    IsConnected: boolean;
    SensorType: SensorTypeEnum;
}

interface IPumpInfo {
    IsOn: boolean;
    DelayToTimeInSeconds : number;
}

interface IStateDto {
    ApiKey: string;
    PumpInfo: IPumpInfo;
    PowerPumpSchedule: ITimeSlot[];
    SensorInfos : ISensorInfoDto[];

}

interface IAjaxCallResult<T> {
    success: boolean;
    data : T;
}

class ScheduleModalController {
    static $inject = ['$scope', '$modalInstance', '$http', 'timeSlotData'];

    constructor($scope: IScheduleModal, $modalInstance, $http : ng.IHttpService, timeSlotData: ng.IHttpPromiseCallbackArg<IAjaxCallResult<IStateDto>>) {
        console.log(timeSlotData);
        $scope.vm = <any>{ };

        $scope.vm.isSaving = false;
        $scope.vm.hasError = false;

        $scope.vm.header = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" ];


        $scope.vm.scheduleTimeSlot = _.map(_.range(1, 25), (h) => {
                                            return _.map($scope.vm.header, (d, dayIndex) => {
                                                if (h == 24) h = 0;
                                                dayIndex++;
                                                if (dayIndex == 7) dayIndex = 0;
                                                return _.find(timeSlotData.data.data.PowerPumpSchedule, { DayOfWeek: dayIndex, Hour: h });
                                            });
                                        });
        console.log($scope.vm.scheduleTimeSlot);
        $scope.vm.onCLickTimeSlotReserseAll = function() {
            _.each(_.range(0, 24), (h) => {
                _.each($scope.vm.header, (d, dayIndex) => {
                    var ts = $scope.vm.scheduleTimeSlot[h][dayIndex];
                    ts.IsOn = !ts.IsOn;
                });
            });
        }

        $scope.vm.onCLickTimeSlotWeekDay = function (day: number) {
            _.each(_.range(0, 24), function(h) {
                var ts = $scope.vm.scheduleTimeSlot[h][day];
                ts.IsOn = !ts.IsOn;
            });
        }

        $scope.vm.onClickTimeSlotHour = function (timeslots: ITimeSlot[]) {
            _.each($scope.vm.header, function (d, dayIndex) {
                var ts = timeslots[dayIndex];
                ts.IsOn = !ts.IsOn;
            });
        }

        $scope.vm.onClickTimeSlot = function (timeSlot: ITimeSlot) {
            console.log("updating cell:", timeSlot, $scope.vm.scheduleTimeSlot);
            var ts = _.find(timeSlotData.data.data.PowerPumpSchedule, { DayOfWeek: timeSlot.DayOfWeek, Hour: timeSlot.Hour });
            ts.IsOn = !ts.IsOn;
        }

        $scope.vm.ok = function () {
            $scope.vm.isSaving = true;
            $scope.vm.hasError = false;
            $http.post('/api/state/UpdatePumpScheduleTimeSlot', _.flatten($scope.vm.scheduleTimeSlot, true))
                .then(() => {
                    $modalInstance.close();
                }, () => {
                    $scope.vm.isSaving = false;
                    $scope.vm.hasError = true;
                });
        };

        $scope.vm.cancel = function () {
            $modalInstance.dismiss('cancel');
        };
    }
}


class AppController {
    static $inject = ['$scope', '$modal', '$log', '$http', '$interval'];
    constructor($scope: IAppModel, $modal, $log, $http, $interval : ng.IIntervalService) {

        $scope.coords = [[869, 568], [143, 590], [724, 481], [157, 345], [399, 548], [651, 520]];

        $scope.vm = <any> {};
        $scope.vm.lastUpdatedOnDateTime = moment("12-25-2995", "MM-DD-YYYY").toDate();
        $scope.vm.lastUpdatedOnDateTimeStr = "not yet available";

        $scope.vm.pumpDelayToInSeconds = 0;
        $scope.vm.pumpDelayToInSecondsStr = "";

        $scope.vm.loading = true;
        $scope.vm.sensorInfo = _.map($scope.coords, function (c : number[], i) { return {id: i+1, coordsXY : c, sensorType : SensorTypeEnum.DrySensor, hasError : false } });

        $scope.vm.showAPIKey = false;

        $scope.vm.toggleAPIKeyDisplay =  function() {
            $scope.vm.showAPIKey = !$scope.vm.showAPIKey;
        }

        $scope.vm.onClickScheduleButton = () => {
            $log.info('clicked on scheduleButton ');

            var modalInstance = $modal.open({
                templateUrl: 'ScheduleModal.html',
                controller: ScheduleModalController,
                size: 'lg',
                resolve: {
                    timeSlotData: function () {
                        return $http({ method: 'GET', url: '/api/state/state/' });
                    }
                }
            });
        }

        $scope.vm.updateSensorInfo = function () {
            console.log('in update');
            $http({ method: 'GET', url: '/api/state/state/' }).
                then(function (stateDto: ng.IHttpPromiseCallbackArg<IAjaxCallResult<IStateDto>>) {

                _.each(stateDto.data.data.SensorInfos, function(sensorDto: ISensorInfoDto) {
                    var sensor = _.find($scope.vm.sensorInfo, { id: sensorDto.Id });
                    if (sensor == null) return;
                    sensor.sensorType = sensorDto.SensorType;
                    sensor.hasError = (sensorDto.SensorType == SensorTypeEnum.DrySensor && sensorDto.IsConnected) ||
                                        (sensorDto.SensorType == SensorTypeEnum.WetSensor && !sensorDto.IsConnected)  ;
                });

                $scope.vm.isReadyOnly = stateDto.data.data.ApiKey.length > 0;
                $scope.vm.pumpDelayToInSeconds = stateDto.data.data.PumpInfo.DelayToTimeInSeconds;
                $scope.vm.pumpIsOn = stateDto.data.data.PumpInfo.IsOn;

                $scope.vm.lastUpdatedOnDateTime = moment().toDate();

                $scope.vm.loading = false;
            }, function() {
                console.log("there is error.");
            });
        }

        $scope.vm.sensorGetLogClicked = function (sensor : ISensorInfo) {
            var modalInstance = $modal.open({
                templateUrl: 'sensorLogModal.html',
                controller: SensorLogModalController,
                size: 'lg',
                resolve: {
                    sensorLogs: function() {
                        return $http({ method: 'GET', url: '/api/state/SensorLog/' + sensor.id });
                    },
                    sensorId: function () { return sensor.id; },
                    sensorType: function() { return sensor.sensorType; }
                }
            });
        }

        function updateTimer() {
            if ($scope.vm.pumpDelayToInSeconds > 0)
                $scope.vm.pumpDelayToInSeconds--;

            $scope.vm.pumpDelayToInSecondsStr = ($scope.vm.pumpDelayToInSeconds > 60 ? Math.floor($scope.vm.pumpDelayToInSeconds / 60).toString() : "0")
                                                    + ":" +($scope.vm.pumpDelayToInSeconds % 60).toString();

            if ($scope.vm.lastUpdatedOnDateTime.valueOf() < moment().toDate().valueOf()) {
                $scope.vm.lastUpdatedOnDateTimeStr = moment($scope.vm.lastUpdatedOnDateTime).fromNow();
            }
        }

        $interval($scope.vm.updateSensorInfo, 5000, 0, true);
        $interval(updateTimer, 1000, 0, true);

    }
}



angular.module('app', ['ui.bootstrap']).controller("appController", AppController);

