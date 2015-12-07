// File globs.

var path = require( 'path' );
var pathToThisFile = __dirname;
var root = path.dirname( pathToThisFile );
var destination = root + '/build/';

var solutionName = 'IdentityServer3.Saml2Bearer';
module.exports =
{
	msbuild : {configuration: 'Debug'},
	paths : {
		solutionFile : "./src/" + solutionName + ".sln",
		}
};