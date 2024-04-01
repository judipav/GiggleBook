--
-- PostgreSQL database dump
--

-- Dumped from database version 15.6 (Debian 15.6-1.pgdg120+2)
-- Dumped by pg_dump version 16.0

-- Started on 2024-04-01 21:07:28

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 3400 (class 1262 OID 16388)
-- Name: gigglebook; Type: DATABASE; Schema: -; Owner: postgres
--

CREATE DATABASE gigglebook WITH TEMPLATE = template0 ENCODING = 'UTF8' LOCALE_PROVIDER = libc LOCALE = 'en_US.utf8';


ALTER DATABASE gigglebook OWNER TO postgres;

\connect gigglebook

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 869 (class 1247 OID 32787)
-- Name: gender; Type: DOMAIN; Schema: public; Owner: postgres
--

CREATE DOMAIN public.gender AS character(1)
	CONSTRAINT gender_check CHECK ((VALUE = ANY (ARRAY['F'::bpchar, 'M'::bpchar])));


ALTER DOMAIN public.gender OWNER TO postgres;

--
-- TOC entry 222 (class 1255 OID 16460)
-- Name: add_friend(uuid, uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.add_friend(current_user_id uuid, friend_user_id uuid) RETURNS boolean
    LANGUAGE plpgsql
    AS $$

declare cnt integer;
begin
	select count(*) into cnt from "Friends" f
	where f.user_id = current_user_id and f.friend_id = friend_user_id;
	
	if (cnt) > 0 then 
		raise exception 'Уже в друзьях';
	end if;
	
	INSERT INTO public."Friends"(
	user_id, friend_id)
	VALUES (current_user_id, friend_user_id);
	
	return true;
end;
$$;


ALTER FUNCTION public.add_friend(current_user_id uuid, friend_user_id uuid) OWNER TO postgres;

--
-- TOC entry 240 (class 1255 OID 32800)
-- Name: auth_user(character, character); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.auth_user(user_name character, hash character) RETURNS TABLE(id uuid, username character varying, first_name character varying, second_name character varying, birthdate date, biography character varying, city character varying, sex public.gender)
    LANGUAGE plpgsql
    AS $$
begin
	return query select u.id, u.username, u.first_name, u.second_name, u.birthdate, u.biography, u.city, u.sex from "User" u
	where u.username = user_name and u.password = hash;
end;
$$;


ALTER FUNCTION public.auth_user(user_name character, hash character) OWNER TO postgres;

--
-- TOC entry 241 (class 1255 OID 32801)
-- Name: find_user(character varying, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.find_user(user_fname character varying, user_sname character varying) RETURNS TABLE(id uuid, username character varying, first_name character varying, second_name character varying, birthdate date, biography character varying, city character varying, sex public.gender)
    LANGUAGE plpgsql
    AS $$
begin
	return query select u.id, u.username, u.first_name, u.second_name, u.birthdate, u.biography, u.city, u.sex from "User" u
	where u.first_name like '%' || user_fname || '%' 
	and u.second_name like '%' || user_sname || '%';
end;
$$;


ALTER FUNCTION public.find_user(user_fname character varying, user_sname character varying) OWNER TO postgres;

--
-- TOC entry 242 (class 1255 OID 32802)
-- Name: get_user(uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.get_user(user_id uuid) RETURNS TABLE(id uuid, username character varying, first_name character varying, second_name character varying, birthdate date, biography character varying, city character varying, sex public.gender)
    LANGUAGE plpgsql
    AS $$
begin
	return query select u.id, u.username, u.first_name, u.second_name, u.birthdate, u.biography, u.city, u.sex from "User" u
	where u.id = user_id;
end;
$$;


ALTER FUNCTION public.get_user(user_id uuid) OWNER TO postgres;

--
-- TOC entry 224 (class 1255 OID 24584)
-- Name: list_message(uuid, uuid, integer, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.list_message(from_uid uuid, to_uid uuid, count_skip integer, take integer) RETURNS TABLE(id uuid, friend_id uuid, msg_text character, datesend date)
    LANGUAGE plpgsql
    AS $$
begin
	return query SELECT d.from, d.to, d.text, d.datesend
	FROM "DialogMessage" d
	where d.from = from_uid and d.to = to_uid
	ORDER BY datesend DESC
	OFFSET (@count_skip) ROWS FETCH NEXT (@Take) ROWS ONLY;
end;
$$;


ALTER FUNCTION public.list_message(from_uid uuid, to_uid uuid, count_skip integer, take integer) OWNER TO postgres;

--
-- TOC entry 237 (class 1255 OID 32782)
-- Name: post_create(uuid, text); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.post_create(author_uid uuid, post_text text) RETURNS TABLE(post_id uuid)
    LANGUAGE plpgsql
    AS $$
begin
	return query INSERT INTO public."Post"( 
	"text", author_user_id)
	VALUES (post_text, author_uid)
	RETURNING id;
	
end;
$$;


ALTER FUNCTION public.post_create(author_uid uuid, post_text text) OWNER TO postgres;

--
-- TOC entry 220 (class 1255 OID 32775)
-- Name: post_delete(uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.post_delete(post_id uuid) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
declare cnt integer;
begin
	select count(*) into cnt from "Post" p
	where p.user_id = post_id;
	
	if (cnt) = 0 then 
		raise exception 'Пост не найден';
	end if;
	
	delete from public."Post"
	WHERE "id" = post_id;
		
	return true;
end;
$$;


ALTER FUNCTION public.post_delete(post_id uuid) OWNER TO postgres;

--
-- TOC entry 239 (class 1255 OID 32783)
-- Name: post_feed(uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.post_feed(r_user_id uuid) RETURNS TABLE(post_id uuid, author_id uuid, post_text text)
    LANGUAGE plpgsql
    AS $$

begin
	RETURN QUERY SELECT p."id", p.author_user_id, p.text
	FROM "Post" p
	LEFT JOIN "Friends" f on f.user_id = r_user_id
	WHERE f.friend_id = p.author_user_id
	or p.author_user_id = r_user_id;
end;
$$;


ALTER FUNCTION public.post_feed(r_user_id uuid) OWNER TO postgres;

--
-- TOC entry 219 (class 1255 OID 32777)
-- Name: post_get(uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.post_get(post_id uuid) RETURNS TABLE(post_text text)
    LANGUAGE plpgsql
    AS $$

begin
	return query select "text" from public."Post" where "id" = post_id;
end;
$$;


ALTER FUNCTION public.post_get(post_id uuid) OWNER TO postgres;

--
-- TOC entry 221 (class 1255 OID 32774)
-- Name: post_update(uuid, text); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.post_update(post_id uuid, post_text text) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
declare cnt integer;
begin
	select count(*) into cnt from "Post" p
	where p.user_id = post_id;
	
	if (cnt) = 0 then 
		raise exception 'Пост не найден';
	end if;

	UPDATE public."Post"
	SET "text" = post_text
	WHERE "id" = post_id;
		
	return true;
end;
$$;


ALTER FUNCTION public.post_update(post_id uuid, post_text text) OWNER TO postgres;

--
-- TOC entry 238 (class 1255 OID 32799)
-- Name: register_user(character, character, date, character, character, character, character, character); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.register_user(f_name character, s_name character, dt_birth date, bio character, city character, sword character, u_name character, u_sex character) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
declare w int;
begin
	select count(*) into w from "User" u
		where u.username = u_name ;
	
	if w > 0 then 
		RAISE EXCEPTION 'login уже существуе';
	end if;
	
	INSERT INTO public."User"(
	first_name, second_name, birthdate, biography, city, password, username, sex)
	VALUES (f_name, s_name, dt_birth, bio, city, sword, u_name, u_sex);
		
	return true; 
end;	
$$;


ALTER FUNCTION public.register_user(f_name character, s_name character, dt_birth date, bio character, city character, sword character, u_name character, u_sex character) OWNER TO postgres;

--
-- TOC entry 225 (class 1255 OID 24586)
-- Name: remove_friend(uuid, uuid); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.remove_friend(current_user_id uuid, friend_user_id uuid) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
declare cnt integer;
begin
	select count(*) into cnt from "Friends" f
	where f.user_id = current_user_id and f.friend_id = friend_user_id;
	
	if (cnt) > 0 then 
		DELETE FROM public."Friends" f
		where f.user_id = current_user_id and friend_id = friend_user_id;
		
		return true;
	end if;
end;
$$;


ALTER FUNCTION public.remove_friend(current_user_id uuid, friend_user_id uuid) OWNER TO postgres;

--
-- TOC entry 223 (class 1255 OID 24582)
-- Name: send_message(uuid, uuid, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.send_message(from_uid uuid, to_uid uuid, msg character varying) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
declare cnt integer;
begin
	select count(*) into cnt from "User" u
	where u.id in (from_uid, to_uid);
	
	if (cnt) <> 2 then
		raise exception 'Пользователя не существует';
	end if;
	
	INSERT INTO public."DialogMessage"(
	"from", "to", text)
	VALUES (from_uid, to_uid, msg);
	
	return true;
end;
$$;


ALTER FUNCTION public.send_message(from_uid uuid, to_uid uuid, msg character varying) OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 216 (class 1259 OID 16423)
-- Name: DialogMessage; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."DialogMessage" (
    "from" uuid NOT NULL,
    "to" uuid NOT NULL,
    text text NOT NULL,
    datesend timestamp with time zone DEFAULT now()
);


ALTER TABLE public."DialogMessage" OWNER TO postgres;

--
-- TOC entry 217 (class 1259 OID 16438)
-- Name: Friends; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Friends" (
    user_id uuid NOT NULL,
    friend_id uuid NOT NULL
);


ALTER TABLE public."Friends" OWNER TO postgres;

--
-- TOC entry 215 (class 1259 OID 16401)
-- Name: Post; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Post" (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    text text NOT NULL,
    author_user_id uuid NOT NULL
);


ALTER TABLE public."Post" OWNER TO postgres;

--
-- TOC entry 214 (class 1259 OID 16389)
-- Name: User; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."User" (
    id uuid DEFAULT gen_random_uuid() NOT NULL,
    first_name character varying(100) NOT NULL,
    second_name character varying(100) NOT NULL,
    birthdate date NOT NULL,
    biography character varying,
    city character varying NOT NULL,
    password character varying NOT NULL,
    username character varying,
    sex public.gender
);


ALTER TABLE public."User" OWNER TO postgres;

--
-- TOC entry 218 (class 1259 OID 16457)
-- Name: cnt; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.cnt (
    count bigint
);


ALTER TABLE public.cnt OWNER TO postgres;

--
-- TOC entry 3392 (class 0 OID 16423)
-- Dependencies: 216
-- Data for Name: DialogMessage; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."DialogMessage" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'ded230f5-cfd6-436c-b47b-415b2e3e801c', 'hello world', '2024-03-03 15:56:20.104373+00');
INSERT INTO public."DialogMessage" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'ded230f5-cfd6-436c-b47b-415b2e3e801c', 'hello world 10', '2024-03-03 16:01:06.19755+00');
INSERT INTO public."DialogMessage" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'ded230f5-cfd6-436c-b47b-415b2e3e801c', 'hello world 10', '2024-03-03 16:01:07.484174+00');
INSERT INTO public."DialogMessage" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'ded230f5-cfd6-436c-b47b-415b2e3e801c', 'hello world 10', '2024-03-03 16:01:08.145831+00');
INSERT INTO public."DialogMessage" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'ded230f5-cfd6-436c-b47b-415b2e3e801c', 'hello world 10', '2024-03-03 16:01:08.597693+00');
INSERT INTO public."DialogMessage" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'ded230f5-cfd6-436c-b47b-415b2e3e801c', 'hello world 10', '2024-03-03 16:01:09.000292+00');
INSERT INTO public."DialogMessage" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'ded230f5-cfd6-436c-b47b-415b2e3e801c', 'hello world 10', '2024-03-03 16:01:09.273904+00');
INSERT INTO public."DialogMessage" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'ded230f5-cfd6-436c-b47b-415b2e3e801c', 'hello world 10', '2024-03-03 16:01:09.57971+00');
INSERT INTO public."DialogMessage" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'ded230f5-cfd6-436c-b47b-415b2e3e801c', 'hello world 10', '2024-03-03 16:01:10.326677+00');
INSERT INTO public."DialogMessage" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'ded230f5-cfd6-436c-b47b-415b2e3e801c', 'hello world 10', '2024-03-03 16:01:10.72289+00');
INSERT INTO public."DialogMessage" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'ded230f5-cfd6-436c-b47b-415b2e3e801c', 'hello world 10', '2024-03-03 16:01:47.222203+00');
INSERT INTO public."DialogMessage" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'ded230f5-cfd6-436c-b47b-415b2e3e801c', 'hello world 10', '2024-03-03 16:01:48.491416+00');
INSERT INTO public."DialogMessage" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'ded230f5-cfd6-436c-b47b-415b2e3e801c', 'hello world 10', '2024-03-03 16:01:49.183019+00');
INSERT INTO public."DialogMessage" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'ded230f5-cfd6-436c-b47b-415b2e3e801c', 'hello world 10', '2024-03-03 16:01:49.637308+00');


--
-- TOC entry 3393 (class 0 OID 16438)
-- Dependencies: 217
-- Data for Name: Friends; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Friends" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'ded230f5-cfd6-436c-b47b-415b2e3e801c');


--
-- TOC entry 3391 (class 0 OID 16401)
-- Dependencies: 215
-- Data for Name: Post; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Post" VALUES ('6e5164fa-f47d-4a29-8f77-0ef5ebf128d2', 'alsyf2451345234625tertt', '1fb7b732-1e21-44c4-bc54-efaf1172e191');
INSERT INTO public."Post" VALUES ('e9752ef3-c392-4032-b92d-454392de7c13', 'alsyfbvakgvkaurevkwy46avcujhdsa', '1fb7b732-1e21-44c4-bc54-efaf1172e191');
INSERT INTO public."Post" VALUES ('bc7f8300-7648-4803-a43d-b913fbf88943', '129487012394yhfqer', 'e061f5f9-4c52-4913-ae90-542e5f641f05');
INSERT INTO public."Post" VALUES ('6a237a82-c118-4475-90ae-b69068cea6ad', '129487012394yhfqer', 'ded230f5-cfd6-436c-b47b-415b2e3e801c');


--
-- TOC entry 3390 (class 0 OID 16389)
-- Dependencies: 214
-- Data for Name: User; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."User" VALUES ('1fb7b732-1e21-44c4-bc54-efaf1172e191', 'pavel', 'yudin', '1987-05-21', 'bio about me', 'samara', 'pwd', 'uname', 'M');
INSERT INTO public."User" VALUES ('ded230f5-cfd6-436c-b47b-415b2e3e801c', 'anna', 'lukoje', '1990-10-01', 'bio about anna', 'togliatti', 'pwd', 'lukojename', 'F');
INSERT INTO public."User" VALUES ('e061f5f9-4c52-4913-ae90-542e5f641f05', 'anna', 'lukoje', '1990-10-01', 'bio about anna', 'togliatti', 'pwd', 'lukojename2', 'F');
INSERT INTO public."User" VALUES ('2f468a18-515d-4783-8763-0b79928d0473', 'Pavel', 'Yudin', '1987-05-21', 'Родился и вырос в Самаре, сердце России, город Жигулевского пива, Волжского гедонизма, бесконечной набережной и волшебных закатов', 'Самара', 'xJUaj07coz4IG6i5hs7ITM4d-EHN15nDdteD9YfIDPk', 'Wolframm', 'M');
INSERT INTO public."User" VALUES ('a9656fcf-7eee-4761-aa9a-ebfb9b75b765', 'Fedor', 'TheCat', '2018-11-01', 'Самый сладкий пирожочек во вселенной', 'Самара', '2jA-Mcx-P3woAPh5gZoPAcZDp6dK7lSjMZdUxTtIwNM', 'theodor', 'M');


--
-- TOC entry 3394 (class 0 OID 16457)
-- Dependencies: 218
-- Data for Name: cnt; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public.cnt VALUES (0);


--
-- TOC entry 3241 (class 2606 OID 16442)
-- Name: Friends friends_user_id_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Friends"
    ADD CONSTRAINT friends_user_id_pkey PRIMARY KEY (user_id);


--
-- TOC entry 3235 (class 2606 OID 16395)
-- Name: User pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."User"
    ADD CONSTRAINT pkey PRIMARY KEY (id);


--
-- TOC entry 3239 (class 2606 OID 16407)
-- Name: Post post_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Post"
    ADD CONSTRAINT post_pkey PRIMARY KEY (id);


--
-- TOC entry 3243 (class 2606 OID 16444)
-- Name: Friends unique_friends; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Friends"
    ADD CONSTRAINT unique_friends UNIQUE (user_id, friend_id);


--
-- TOC entry 3237 (class 2606 OID 16452)
-- Name: User username_loginname; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."User"
    ADD CONSTRAINT username_loginname UNIQUE (username);


--
-- TOC entry 3245 (class 2606 OID 16428)
-- Name: DialogMessage dialog_from_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DialogMessage"
    ADD CONSTRAINT dialog_from_user_id_fkey FOREIGN KEY ("from") REFERENCES public."User"(id) ON DELETE CASCADE;


--
-- TOC entry 3246 (class 2606 OID 16433)
-- Name: DialogMessage dialog_to_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."DialogMessage"
    ADD CONSTRAINT dialog_to_user_id_fkey FOREIGN KEY ("to") REFERENCES public."User"(id);


--
-- TOC entry 3247 (class 2606 OID 16445)
-- Name: Friends friend_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Friends"
    ADD CONSTRAINT friend_id_fkey FOREIGN KEY (friend_id) REFERENCES public."User"(id);


--
-- TOC entry 3244 (class 2606 OID 16408)
-- Name: Post post_author_uid; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Post"
    ADD CONSTRAINT post_author_uid FOREIGN KEY (author_user_id) REFERENCES public."User"(id) ON DELETE CASCADE;


-- Completed on 2024-04-01 21:07:29

--
-- PostgreSQL database dump complete
--

