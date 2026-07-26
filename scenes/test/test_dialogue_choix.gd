# Test de traversée scripté du dialogue à choix (lancé en headless :
# `godot --headless scenes/test/test_dialogue_choix.tscn`). Il instancie monde1,
# téléporte Glooby contre le lutin gréviste puis martèle « action » pour dérouler
# toute la conversation, en imprimant l'état à chaque appui.
#
# Ce qu'il attend : le rappel de touche s'ouvre bien sur un PNJ à Conversation
# (DialogueModal passe à vrai après le 1er appui), le don de poissons débite la
# réserve, et surtout le modal FINIT PAR SE RELÂCHER — un modal resté armé fige
# Glooby pour de bon, c'est le seul vrai risque de ce système.
extends Node2D

var monde
var joueur
var lutin
var etapes = 0
var libere = false
var a_ete_modal = false

func _ready():
	monde = load("res://scenes/niveaux/monde1.tscn").instantiate()
	add_child(monde)
	await get_tree().process_frame
	joueur = monde.get_node("Joueur")
	lutin = monde.get_node("LutinCgt")
	joueur.global_position = lutin.global_position + Vector2(24, -8)
	print("TEST> ollama_disponible=", get_node("/root/OllamaService").Disponible)

func _process(_delta):
	etapes += 1
	var gs = get_node("/root/GameState")

	# Un appui toutes les 120 frames (~3 s) : laisse à Ollama le temps de générer : démarrage, répliques, validation des choix.
	if etapes % 180 == 0 and etapes < 3600:
		Input.action_press("action")
		print("TEST> f", etapes, " appui | modal=", gs.DialogueModal, " poissons=", gs.Poissons)
	elif etapes % 180 == 2:
		Input.action_release("action")

	if gs.DialogueModal:
		a_ete_modal = true
	if a_ete_modal and not gs.DialogueModal and not libere:
		libere = true
		print("TEST> modal relâché à la frame ", etapes, " | poissons=", gs.Poissons)

	if etapes == 3600:
		print("TEST> FIN | modal_relache_au_moins_une_fois=", libere)
		get_tree().quit()
