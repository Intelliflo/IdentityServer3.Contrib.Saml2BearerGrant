// v0.1.2  
// Gulp 4.0 file. 
// Install gulp: http://hazmi.id/gulp-4-is-nearly-there-you-can-start-using-it/
// Example v4 file: https://gist.github.com/demisx/beef93591edc1521330a

// Require
var gulp = require("gulp");
var exec = require('child_process').exec;
var shell = require('gulp-shell')
var sprintf = require("sprintf-js").sprintf;
var msbuild = require("gulp-msbuild");
var projectSettings = require("./gulpfile.config.js");
var assign = require("object-assign-deep");
var util = require("gulp-util");

//var xeditor = require("gulp-xml-editor");
var xmlEdit = require('gulp-edit-xml');

var nunit = require('gulp-nunit-runner');
var del = require('del');
var url = require('url');

// constants 

// build default settings, takes a required parameter microservice
var defaultsSettings = function(microservice, projectSettings){
	var configuration = 'Debug';
	var getMsBuildBinFolder = function (configuration) { return sprintf('%s/bin/%s', microservice, configuration);};
	return 	{
		microservice : microservice,
		dbprofile: 'subsys',
		msbuild : {
			configuration : configuration
		},
		paths : {
			solutionFile : "./" + microservice + ".sln",
			binFolder : getMsBuildBinFolder(configuration),
			binFile : sprintf('%s/%s.exe', getMsBuildBinFolder(configuration), microservice),
		},
		nunit: {
			'package': 'NUnit.Runners',
			'version': '2.6.4',
			unitTestAssemblies: sprintf('**/bin/%s/**/*.Tests.dll', configuration),
		},
		};
}(projectSettings.microservice, projectSettings); // pass microservice name from gulp config as microservice name

// update settings with any manually assigned value from gulp config.
var settings = assign(defaultsSettings, projectSettings);

// helper function to dump objects properties and valeus
var dumpObject = function(o){
	for(var propName in o) {
		var propValue = o[propName]
		console.log("propname:'" + propName + "'", 'value:'+propValue);
	}
}

// compiles using msbuild
gulp.task("compile", function() {
    return gulp.src(settings.paths.solutionFile)
        .pipe(msbuild({
			toolsVersion: 14.0,
			targets: ['Build'],
			logCommand : true,
			errorOnFail: true,
			stdout: true,
			properties: { Configuration: settings.msbuild.configuration }
		}));
});

gulp.task("msbuild-clean", function() {
    return gulp.src(settings.paths.solutionFile)
        .pipe(msbuild({
			targets: ['Clean'],
			properties: { Configuration: settings.msbuild.configuration }
		}));
});

gulp.task('nunit', function () {
    return gulp
        .src([settings.nunit.unitTestAssemblies], { read: false })
        .pipe(nunit({
			executable: sprintf('./packages/%s.%s/tools/nunit-console.exe', settings.nunit.package, settings.nunit.version),
            //options : {teamcity: true}
        }));
});

gulp.task('restore-nunit-runner', shell.task([
	sprintf('nuget install %s -OutputDirectory %s -source %s -Version %s', settings.nunit.package, 'packages', '"https://www.nuget.org/api/v2"', settings.nunit.version)
]));

// calls 'nuget restore <microservice>.sln'
gulp.task('nuget-restore', shell.task([
  sprintf('nuget restore %s', settings.paths.solutionFile)
]))

gulp.task('build-clean', function(callback) {
	//return gulp.src(sprintf('%s/*', binFolder))
    //.pipe(vinylPaths(del))
    //.pipe(gulp.dest('dist'));
    //del(binFolder, callback);
	callback(null);
});

// do a build without clean
gulp.task('build', gulp.series('nuget-restore', 'compile'));

// builds and runs tests
gulp.task('test', gulp.series('build', 'restore-nunit-runner', 'nunit'));

// do a full rebuild (will clean output dir first)
gulp.task('rebuild', gulp.series('nuget-restore', 'build-clean', 'compile'));

// builds and runs
gulp.task('default', gulp.series('test'));

