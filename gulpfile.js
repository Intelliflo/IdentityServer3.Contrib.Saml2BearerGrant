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
var assign = require("object-assign-deep");
var util = require("gulp-util");
var mkdirp = require('mkdirp');
var fs = require('fs');

var nunit = require('gulp-nunit-runner');
var del = require('del');
var url = require('url');

var RequireOrCreateEmpty = function(filePath) {
	
	try {
		fs.lstatSync(filePath);
	}
	catch (e) {
		// file does not exist
		fs.writeFileSync(filePath, 'module.exports = {NugetApiKey : "<your nuget API key"};');
	}
	return require(filePath);
}
var projectSettings = RequireOrCreateEmpty("./gulpfile.config.js")


// build default settings, takes a required parameter projectName
var defaultSettings = function(projectName, projectSettings){
	var configuration = 'Debug';
	var getMsBuildBinFolder = function (configuration) { return sprintf('%s/bin/%s', projectName, configuration);};
	return 	{
		projectName : projectName,
		dbprofile: 'subsys',
		msbuild : {
			configuration : configuration
		},
		paths : {
			solutionFile : "./src/" + projectName + ".sln",
			binFolder : getMsBuildBinFolder(configuration),
			binFile : sprintf('%s/%s.exe', getMsBuildBinFolder(configuration), projectName),
		},
		nunit: {
			'package': 'NUnit.Runners',
			'version': '2.6.4',
			unitTestAssemblies: sprintf('**/bin/%s/**/*.Tests.dll', configuration),
		},
		nuget : {
			version : '0.0.2',
			packageName : projectName,
			path : 'nuget',
			spec : function(s) {return sprintf ('%s.nuspec', s.nuget.packageName);},
			nupkgFile : function(s) {return sprintf('%s/%s.%s.nupkg', s.nuget.nupkgFolder(s), s.nuget.packageName, s.nuget.version);},
			nupkgFolder : function(s) {return sprintf('packages');},
			source : '', // use custom nuget source if needed. Include -s option here '-s http://nuget.org'
			auth : {
				ApiKey : projectSettings.NugetApiKey,
			}
		}
	};
}('IdentityServer3.Saml2Bearer', projectSettings); // pass projectName name from gulp config as projectName name

var settings = defaultSettings;



// Creating packages folder for nuget
mkdirp(settings.nuget.nupkgFolder(settings, function (err) {
    if (err) console.error(err)
    else console.log('pow!');
}));

gulp.task('nuget-pack', shell.task([
	sprintf('nuget pack %s -OutputDirectory %s -Version %s', settings.nuget.spec(settings), settings.nuget.nupkgFolder(settings), settings.nuget.version )
]));

gulp.task('nuget-push', shell.task([
	sprintf('nuget push %s %s %s', settings.nuget.nupkgFile(settings), settings.nuget.auth.ApiKey, settings.nuget.source )
]));




// helper function to dump objects properties and values
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

gulp.task('nuget-restore', shell.task([
  sprintf('nuget restore %s', settings.paths.solutionFile)
]))

gulp.task('build-clean', function(callback) {
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

