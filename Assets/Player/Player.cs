using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum statuses {
	STATUS_KIND_WAIT, //Idle
	STATUS_KIND_WALK, //Basic ground movement
	STATUS_KIND_CROUCH_D, //Starting to crouch
	STATUS_KIND_CROUCH, //Staying in a crouch
	STATUS_KIND_CROUCH_U, //Rising from a crouch
	//To clarify, I've worked with crouch animations before and as weird as having 3 different states for crouching is, it's 
	//VERY important for basic movement to feel smooth, and this is the best way to do that
	STATUS_KIND_JUMP_SQUAT, //Prejump frames, basically
	STATUS_KIND_JUMP, //Actually jumping
	STATUS_KIND_FALL, //Falling, either after a jump or walking off a platform
	STATUS_KIND_HITSTUN,
	STATUS_KIND_LANDING,
	STATUS_KIND_MAX, //If I ever turn this into a jumptable, having a value that just says "This is the maximum value for the
	//status list" is really convenient
};

public enum situations {
	SITUATION_KIND_GROUND,
	SITUATION_KIND_AIR,
	SITUATION_KIND_MAX,
};

public enum buttons {
	BUTTON_UP,
	BUTTON_DOWN,
	BUTTON_LEFT,
	BUTTON_RIGHT,
	BUTTON_JUMP,

	BUTTON_MAX,
};

public struct Button {
	public KeyCode mapping;
	public bool on;
	public bool changed;
};

public class Player : MonoBehaviour {
	public Rigidbody2D body; //Unity uses this for physics, positions etc.
	public Collider2D bounds;
	public float health;
	public float max_health;
	public float ammo; //idk what ammo looks like for jokes but we should prob include it
	public float max_ammo;
	public bool facing_right;
	public float reticle_angle; //Gonna use this for aiming 
	public statuses status_kind; //The state the player is in (Jumping, taking damage, etc.)
	public situations situation_kind; //If the player is in the air or on the ground
	public double frame; //The frame of the player's current animation
	public double x_speed;
	public double air_y_speed; //The player's vertical speed
	public double gravity; //How much the player's vertical speed drops by on every frame
	public double air_max_x_speed;
	public double init_jump_x_speed;
	public double init_jump_speed;
	public double walk_speed;
	public double max_fall_speed; //How fast the player can be falling before gravity stops having an effect
	public Button[] button = new Button[(int)buttons.BUTTON_MAX]; //i've been working on this for ~3 hours and friendship already
	//ended with unity
	public GameObject[] platforms;

	// Start is called before the first frame update
	void Start() {
		body = GetComponent<Rigidbody2D> ();
		bounds = GetComponent<Collider2D> ();
		map_buttons();
		health = max_health;  
		ammo = max_ammo;
		gravity = 0.09;
		init_jump_speed = 3.5;
		init_jump_x_speed = 0.7;
		max_fall_speed = -2.0;
		air_max_x_speed = 2.0;
		walk_speed = 1.2;
		status_kind = statuses.STATUS_KIND_WAIT;
	}

	void map_buttons() {
		button[(int)buttons.BUTTON_UP].mapping = KeyCode.W;
		button[(int)buttons.BUTTON_LEFT].mapping = KeyCode.A;
		button[(int)buttons.BUTTON_DOWN].mapping = KeyCode.S;
		button[(int)buttons.BUTTON_RIGHT].mapping = KeyCode.D;
		button[(int)buttons.BUTTON_JUMP].mapping = KeyCode.Space;
		for (int i = 0; i < (int)buttons.BUTTON_MAX; i++) {
			button[i].on = false;
			button[i].changed = false;
		}
	}

	void poll_buttons() {
		for (int i = 0; i < (int)buttons.BUTTON_MAX; i++) {
			bool old_button = button[i].on;
			button[i].on = Input.GetKey(button[i].mapping);
			button[i].changed = button[i].on != old_button;
		}
	}

	bool check_button_on(buttons index) {
		return button[(int)index].on;
	}

	bool check_button_trigger(buttons index) {
		return button[(int)index].on && button[(int)index].changed;
	}

	bool check_button_release(buttons index) {
		return (!button[(int)index].on) && button[(int)index].changed;
	}

	void update_platforms() {
		platforms = GameObject.FindGameObjectsWithTag("Platform");
	}

	bool handle_platforms() {
		int index = get_platform_index();
		if (index == -1) {
			if (situation_kind == situations.SITUATION_KIND_GROUND && body.position.y - (bounds.bounds.size.y / 2.0) > -5.0) {
				change_status(statuses.STATUS_KIND_FALL);
				Debug.Log("L + ratio + you should be falling off");
				return true;
			}
		}
		if (situation_kind == situations.SITUATION_KIND_AIR) {
			if (index != -1) {
				set_pos(body.position.x, platforms[index].GetComponent<Rigidbody2D>().position.y + platforms[index].GetComponent<BoxCollider2D>().bounds.size.y + .1);
				change_status(statuses.STATUS_KIND_LANDING);
				return true;
			}
			if (body.position.y - (bounds.bounds.size.y / 2.0) <= -5.0) {
				set_pos(body.position.x, -5.0 + bounds.bounds.size.y / 2.0);
				change_status(statuses.STATUS_KIND_LANDING);
				return true;
			}
		}
		return false;
	}

	int get_platform_index() {
		for (int i = 0; i < platforms.Length; i++) {
			if (
				(
					body.position.x <= (platforms[i].GetComponent<Rigidbody2D>().position.x + (platforms[i].GetComponent<BoxCollider2D>().bounds.size.x / 2)) &&
					body.position.x >= (platforms[i].GetComponent<Rigidbody2D>().position.x - (platforms[i].GetComponent<BoxCollider2D>().bounds.size.x / 2))
				) 
				&& 
				(
					body.position.y <= (platforms[i].GetComponent<Rigidbody2D>().position.y + platforms[i].GetComponent<BoxCollider2D>().bounds.size.y + .15) &&
					body.position.y >= (platforms[i].GetComponent<Rigidbody2D>().position.y + platforms[i].GetComponent<BoxCollider2D>().bounds.size.y)
				)
				) {
				return i;
			}
		}
		return -1;
	}

	// FixedUpdate is called once per frame
	void FixedUpdate() {
		frame++;
		update_platforms();
		poll_buttons();

		process_aim();
		process_inputs();
		execute_status();
	}

	bool can_act() {
		if (status_kind == statuses.STATUS_KIND_JUMP_SQUAT
		|| status_kind == statuses.STATUS_KIND_HITSTUN) {
			return false;
		}
		return true;
	}

	void process_aim() {

	}

	void process_inputs() {

	}

	void change_status(statuses new_status_kind) {
		frame = 0.0;
		execute_exit_status();
		status_kind = new_status_kind;
		execute_entry_status();
	}

	void execute_status() {
		switch(status_kind) {
			case (statuses.STATUS_KIND_WAIT): {
				status_wait();
			} break;
			case (statuses.STATUS_KIND_WALK): {
				status_walk();
			} break;
			case (statuses.STATUS_KIND_CROUCH_D): {
				status_crouch_d();
			} break;
			case (statuses.STATUS_KIND_CROUCH): {
				status_crouch();
			} break;
			case (statuses.STATUS_KIND_CROUCH_U): {
				status_crouch_u();
			} break;
			case (statuses.STATUS_KIND_JUMP): {
				status_jump();
			} break;
			case (statuses.STATUS_KIND_JUMP_SQUAT) : {
				status_jumpsquat();
			} break;
			case (statuses.STATUS_KIND_FALL): {
				status_fall();
			} break;
			case (statuses.STATUS_KIND_HITSTUN): {
				status_hitstun();
			} break;
			case (statuses.STATUS_KIND_LANDING) : {
				status_landing();
			} break;
			default: {

			} break;
		}
	}

	void execute_entry_status() {
			switch(status_kind) {
			case (statuses.STATUS_KIND_WAIT): {
				entry_status_wait();
			} break;
			case (statuses.STATUS_KIND_WALK): {
				entry_status_walk();
			} break;
			case (statuses.STATUS_KIND_CROUCH_D): {
				entry_status_crouch_d();
			} break;
			case (statuses.STATUS_KIND_CROUCH): {
				entry_status_crouch();
			} break;
			case (statuses.STATUS_KIND_CROUCH_U): {
				entry_status_crouch_u();
			} break;
			case (statuses.STATUS_KIND_JUMP): {
				entry_status_jump();
			} break;
			case (statuses.STATUS_KIND_JUMP_SQUAT) : {
				entry_status_jumpsquat();
			} break;
			case (statuses.STATUS_KIND_FALL): {
				entry_status_fall();
			} break;
			case (statuses.STATUS_KIND_HITSTUN): {
				entry_status_hitstun();
			} break;
			case (statuses.STATUS_KIND_LANDING) : {
				entry_status_landing();
			} break;
			default: {

			} break;
		}
	}

	void execute_exit_status() {
		switch(status_kind) {
			case (statuses.STATUS_KIND_WAIT): {
				exit_status_wait();
			} break;
			case (statuses.STATUS_KIND_WALK): {
				exit_status_walk();
			} break;
			case (statuses.STATUS_KIND_CROUCH_D): {
				exit_status_crouch_d();
			} break;
			case (statuses.STATUS_KIND_CROUCH): {
				exit_status_crouch();
			} break;
			case (statuses.STATUS_KIND_CROUCH_U): {
				exit_status_crouch_u();
			} break;
			case (statuses.STATUS_KIND_JUMP): {
				exit_status_jump();
			} break;
			case (statuses.STATUS_KIND_JUMP_SQUAT) : {
				exit_status_jumpsquat();
			} break;
			case (statuses.STATUS_KIND_FALL): {
				exit_status_fall();
			} break;
			case (statuses.STATUS_KIND_HITSTUN): {
				exit_status_hitstun();
			} break;
			case (statuses.STATUS_KIND_LANDING) : {
				exit_status_landing();
			} break;
			default: {

			} break;
		}
	}

	void add_pos(double x, double y) {
		Vector2 move;
		move.x = (float)x;
		move.y = (float)y;
		body.MovePosition(body.position + move * Time.fixedDeltaTime);
	}

	void set_pos(double x, double y) {
		Vector2 move;
		move.x = (float)x;
		move.y = (float)y;
		body.MovePosition(move);
	}
	
	void shoot() {

	}

	double clamp_d(double min, double val, double max) {
		if (val < min) {
			return min;
		}
		else if (val > max) {
			return max;
		}
		else {
			return val;
		}
	}

	/*

  /$$$$$$  /$$$$$$$$ /$$$$$$  /$$$$$$$$ /$$   /$$  /$$$$$$         /$$$$$$   /$$$$$$  /$$$$$$$  /$$$$$$ /$$$$$$$  /$$$$$$$$ /$$$$$$ 
 /$$__  $$|__  $$__//$$__  $$|__  $$__/| $$  | $$ /$$__  $$       /$$__  $$ /$$__  $$| $$__  $$|_  $$_/| $$__  $$|__  $$__//$$__  $$
| $$  \__/   | $$  | $$  \ $$   | $$   | $$  | $$| $$  \__/      | $$  \__/| $$  \__/| $$  \ $$  | $$  | $$  \ $$   | $$  | $$  \__/
|  $$$$$$    | $$  | $$$$$$$$   | $$   | $$  | $$|  $$$$$$       |  $$$$$$ | $$      | $$$$$$$/  | $$  | $$$$$$$/   | $$  |  $$$$$$ 
 \____  $$   | $$  | $$__  $$   | $$   | $$  | $$ \____  $$       \____  $$| $$      | $$__  $$  | $$  | $$____/    | $$   \____  $$
 /$$  \ $$   | $$  | $$  | $$   | $$   | $$  | $$ /$$  \ $$       /$$  \ $$| $$    $$| $$  \ $$  | $$  | $$         | $$   /$$  \ $$
|  $$$$$$/   | $$  | $$  | $$   | $$   |  $$$$$$/|  $$$$$$/      |  $$$$$$/|  $$$$$$/| $$  | $$ /$$$$$$| $$         | $$  |  $$$$$$/
 \______/    |__/  |__/  |__/   |__/    \______/  \______/        \______/  \______/ |__/  |__/|______/|__/         |__/   \______/ 

	*/

	bool common_ground_status_act() {
		if (handle_platforms()) {
			return true;
		}
		if (check_button_trigger(buttons.BUTTON_RIGHT) || check_button_trigger(buttons.BUTTON_LEFT)) {
			facing_right = check_button_trigger(buttons.BUTTON_RIGHT);
			change_status(statuses.STATUS_KIND_WALK);
			return true;
		}
		else {
			if (x_speed > 0.0) {
				x_speed = clamp_d(0.0, x_speed - 0.06, x_speed);
			}
			else if (x_speed < 0.0) {
				x_speed = clamp_d(x_speed, x_speed + 0.06, 0.0);
			}
			add_pos(x_speed, 0.0);
		}
		if (check_button_trigger(buttons.BUTTON_JUMP)) {
			change_status(statuses.STATUS_KIND_JUMP_SQUAT);
			return true;
		}
		if (check_button_trigger(buttons.BUTTON_DOWN)) {
			change_status(statuses.STATUS_KIND_CROUCH_D);
			return true;
		}
		return false;
	}

	void apply_air_movement() {
		if (facing_right) {
			if (check_button_trigger(buttons.BUTTON_LEFT)) {
				if (x_speed > 0.0) {
					x_speed *= -1.0;
				}
				facing_right = false;
			}
		}
		else {
			if (check_button_trigger(buttons.BUTTON_RIGHT)) {
				if (x_speed < 0.0) {
					x_speed *= -1.0;                    
				}
				facing_right = true;
			}
		}
		if (check_button_on(buttons.BUTTON_RIGHT)) {
			if (x_speed < air_max_x_speed) {
				x_speed = clamp_d(x_speed, x_speed + 0.01, air_max_x_speed);
			}
		}
		if (check_button_on(buttons.BUTTON_LEFT)) {
			if (x_speed > air_max_x_speed * -1.0) {
				x_speed = clamp_d(air_max_x_speed * -1.0, x_speed - 0.01, x_speed);
			}
		}
		if (air_y_speed > max_fall_speed) {
			air_y_speed = clamp_d(max_fall_speed, air_y_speed - gravity, air_y_speed);
		}
		add_pos(x_speed, air_y_speed);
	}

	void status_wait() {
		if (common_ground_status_act()) {
			return;
		}
		 if (check_button_on(buttons.BUTTON_RIGHT) || check_button_on(buttons.BUTTON_LEFT)) {
			facing_right = check_button_on(buttons.BUTTON_RIGHT);
			change_status(statuses.STATUS_KIND_WALK);
		}
	}

	void entry_status_wait() {

	}

	void exit_status_wait() {

	}

	void status_walk() {
		if (common_ground_status_act()) {
			return;
		}
		if (check_button_on(buttons.BUTTON_RIGHT)) {
			add_pos(walk_speed, 0.0);
			x_speed = 1.2;
		}
		else if (check_button_on(buttons.BUTTON_LEFT)) {
			add_pos(walk_speed * -1.0, 0.0);
			x_speed = -1.2;
		}
		else {
			change_status(statuses.STATUS_KIND_WAIT);
		}
	}

	void entry_status_walk() {

	}

	void exit_status_walk() {

	}

	void status_crouch_d() {
		if (common_ground_status_act()) {
			return;
		}
		if (!check_button_on(buttons.BUTTON_DOWN)) {
			change_status(statuses.STATUS_KIND_CROUCH_U);
		}
		if (frame > 3.0) {
			change_status(statuses.STATUS_KIND_CROUCH);
		}
	}

	void entry_status_crouch_d() {

	}

	void exit_status_crouch_d() {

	}

	void status_crouch() {
		if (common_ground_status_act()) {
			return;
		}
		if (!check_button_on(buttons.BUTTON_DOWN)) {
			change_status(statuses.STATUS_KIND_CROUCH_U);
		}
	}

	void entry_status_crouch() {

	}

	void exit_status_crouch() {

	}

	void status_crouch_u() {
		if (common_ground_status_act()) {
			return;
		}
		if (frame > 3.0) {
			change_status(statuses.STATUS_KIND_WAIT);
		}
	}

	void entry_status_crouch_u() {

	}

	void exit_status_crouch_u() {

	}

	void status_jump() {
		apply_air_movement();

		if (air_y_speed < 0.0) {
			change_status(statuses.STATUS_KIND_FALL);
		}
	}

	void entry_status_jump() {
		if (check_button_on(buttons.BUTTON_RIGHT)) {
			x_speed = 3.0;
		}
		else if (check_button_on(buttons.BUTTON_LEFT)) {
			x_speed = -3.0;
		}
		else {

		}
		situation_kind = situations.SITUATION_KIND_AIR;
		air_y_speed = init_jump_speed;
	}

	void exit_status_jump() {

	}

	void status_jumpsquat() {
		if (check_button_trigger(buttons.BUTTON_RIGHT)) {
			facing_right = true;
		}
		if (check_button_trigger(buttons.BUTTON_LEFT)) {
			facing_right = false;
		}
		bool is_air_speed_right = x_speed >= 0;
		if (is_air_speed_right != facing_right) { //I thought it'd be cool if jumping backwards was slightly faster than jumping
		//forwards so fuck you it's in the game now
			x_speed *= 1.16;
		}
		add_pos(x_speed, 0.0);
		if (frame == 3.0) {
			change_status(statuses.STATUS_KIND_JUMP);
		}
	}

	void entry_status_jumpsquat() {
		x_speed *= init_jump_x_speed;
	}

	void exit_status_jumpsquat() {

	}

	void status_fall() {
		if (handle_platforms()) {
			return;
		}
		apply_air_movement();
	}

	void entry_status_fall() {
		situation_kind = situations.SITUATION_KIND_AIR;
	}

	void exit_status_fall() {

	}

	void status_hitstun() {

	}

	void entry_status_hitstun() {

	}

	void exit_status_hitstun() {
		
	}

	void status_landing() {
		if (common_ground_status_act()) {
			return;
		}
		if ((facing_right && check_button_on(buttons.BUTTON_RIGHT)) || (!facing_right && check_button_on(buttons.BUTTON_LEFT))) {
			//Basically, if you hold in the direction you were already facing, landing will cancel immediately into a walk,
			//but if you want to walk in the direction you AREN'T facing, you need to tap the button as you land. This makes
			//movement feel smooth while also giving the player some satisfaction if they get good at turning on a dime.
			change_status(statuses.STATUS_KIND_WALK);
			return;
		}
		if (frame > 3.0) {
			change_status(statuses.STATUS_KIND_WAIT);
		}
	}

	void entry_status_landing() {
		situation_kind = situations.SITUATION_KIND_GROUND;
	}

	void exit_status_landing() {

	}
}
