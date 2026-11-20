using System;

namespace Shooter.Infrastructure;

public class ReadableIdentityMemoryAnimalsDict : ReadableIdentityMemoryDict
{
    private readonly string[] content =
{
    "aardvark", "aardwolf", "albatross", "alligator", "alpaca", "amphibian", "anaconda",
    "angelfish", "anglerfish", "ant", "anteater", "antelope", "antlion", "robin", "aphid",
    "armadillo", "asp", "seal", "badger", "bandicoot", "barnacle", "barracuda",
    "basilisk", "bass", "bat", "bear", "kestrel", "osprey", "bee", "beetle", "bird",
    "bison", "blackbird", "boa", "boar", "bobcat", "bobolink", "bonobo", "ibis",
    "bovid", "bug", "butterfly", "buzzard", "camel", "canid", "canidae", "capybara",
    "cardinal", "caribou", "carp", "cat", "caterpillar", "catfish", "catshark",
    "pika", "centipede", "cephalopod", "chameleon", "cheetah", "chickadee",
    "starling", "chimpanzee", "chinchilla", "chipmunk", "cicada", "clam", "clownfish",
    "cobra", "plover", "cod", "condor", "constrictor", "coral", "cougar", "jackdaw",
    "coyote", "crab", "crane", "crawdad", "crayfish", "cricket", "crocodile", "crow",
    "manta", "damselfly", "deer", "dingo", "dinosaur", "dog", "dolphin", "beluga",
    "dormouse", "dove", "dragon", "dragonfly", "duck", "eagle", "sandpiper", "earwig",
    "echidna", "eel", "egret", "elephant", "elk", "emu", "ermine", "falcon",
    "felidae", "ferret", "finch", "firefly", "fish", "flamingo", "sable", "fly",
    "flyingfish", "fowl", "fox", "frog", "galliform", "gamefowl", "lapwing", "gazelle",
    "gecko", "gerbil", "gibbon", "giraffe", "stallion", "goldfish", "goose", "gopher",
    "bluebird", "grasshopper", "grouse", "guan", "guanaco", "guineafowl", "gull",
    "guppy", "haddock", "halibut", "hamster", "hare", "harrier", "hawk", "hedgehog",
    "heron", "herring", "collie", "beagle", "hornet", "horse", "hoverfly",
    "hummingbird", "corgi", "iguana", "impala", "fennec", "jaguar", "jay",
    "jellyfish", "junglefowl", "kangaroo", "kingfisher", "kite", "kiwi", "koala",
    "koi", "krill", "ladybug", "lamprey", "landfowl", "lark", "caracal", "lemming",
    "lemur", "leopard", "leopon", "limpet", "lion", "lizard", "llama", "lobster",
    "locust", "loon", "serval", "lungfish", "lynx", "macaw", "mackerel", "magpie",
    "mammal", "manatee", "mandrill", "marlin", "marmoset", "marmot", "marsupial",
    "marten", "mastodon", "meadowlark", "meerkat", "mink", "minnow", "axolotl",
    "mockingbird", "mole", "mollusk", "mongoose", "dugong", "moose", "mosquito",
    "moth", "mouse", "mallard", "muskox", "narwhal", "newt", "nightingale", "ocelot",
    "octopus", "opossum", "orangutan", "orca", "ostrich", "otter", "owl", "ox",
    "panda", "panther", "parakeet", "parrot", "parrotfish", "partridge", "peacock",
    "peafowl", "pelican", "penguin", "perch", "pheasant", "gannet", "pigeon", "pike",
    "pinniped", "piranha", "planarian", "platypus", "pony", "porcupine", "porpoise",
    "possum", "prawn", "primate", "ptarmigan", "puffin", "puma", "python", "quail",
    "quelea", "quokka", "rabbit", "raccoon", "petrel", "rattlesnake", "raven",
    "reindeer", "reptile", "rhinoceros", "roadrunner", "rodent", "rook", "oriole",
    "goldfinch", "sailfish", "salamander", "salmon", "sawfish", "scallop",
    "scorpion", "seahorse", "shark", "lyrebird", "shrew", "shrimp", "silkworm",
    "silverfish", "skink", "kookaburra", "sloth", "okapi", "smelt", "snail", "pangolin",
    "snipe", "sole", "sparrow", "spider", "spoonbill", "squid", "squirrel",
    "starfish", "stingray", "stoat", "stork", "sturgeon", "springbok", "swan", "swift",
    "swordfish", "swordtail", "tahr", "takin", "tapir", "tarantula", "tarsier",
    "termite", "tern", "thrush", "ibex", "tiger", "tiglon", "kakapo", "tortoise",
    "toucan", "trout", "tuna", "turkey", "turtle", "tyrannosaurus", "unicorn",
    "urial", "vicuna", "grebe", "vole", "cormorant", "wallaby", "walrus", "warbler",
    "wasp", "tanager", "whale", "whippet", "whitefish", "wildcat", "wildebeest",
    "wildfowl", "wolf", "wolverine", "wombat", "bunting", "kingbird", "wren",
    "xerinae", "yak", "zebra"
};


    public override ReadOnlySpan<string> Words => content;
}
