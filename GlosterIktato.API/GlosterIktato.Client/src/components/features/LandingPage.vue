<template>
	<AppLayout
		:nav-items="navItems"
		:companies="companies"
		:selected-company-id="selectedCompanyId"
		@update:selectedCompanyId="(val) => selectedCompanyId = val"
		@open-user-menu="logout"
	>
		<div class="space-y-4">
			<h1 class="text-2xl font-semibold text-gray-900">Üdvözöllek a Gloster Iktatórendszerben, <span class="font-medium">{{ auth.userName }}</span>!</h1>
			<div class="mb-8">
            <h2 class="text-2xl font-semibold text-gray-900 mb-3">Mi ez?</h2>
            <p class="text-gray-700 text-lg leading-relaxed">
                A <span class="font-semibold text-blue-600">GlosterIktato Vueserossz</span> verziója egy modern, digitális iktatórendszer, amely leegyszerűsíti a cégcsoport dokumentumainak kezelését, jóváhagyását és nyilvántartását.
            </p>
        </div>

        <!-- Mit tudsz vele csinálni? -->
        <div class="mb-8">
            <h2 class="text-2xl font-semibold text-gray-900 mb-4">Mit tudsz vele csinálni?</h2>
            
            <div class="space-y-4">
                <!-- Feature 1 -->
                <div class="flex items-start gap-4 p-4 bg-blue-50 rounded-lg">
                    <div class="flex-shrink-0 w-10 h-10 bg-blue-600 rounded-lg flex items-center justify-center">
                        <i class="fas fa-file-upload text-white"></i>
                    </div>
                    <div>
                        <h3 class="font-semibold text-gray-900 mb-1">Dokumentumok feltöltése</h3>
                        <p class="text-gray-600">Számlák, szerződések, igazolások egyszerű feltöltése</p>
                    </div>
                </div>

                <!-- Feature 2 -->
                <div class="flex items-start gap-4 p-4 bg-green-50 rounded-lg">
                    <div class="flex-shrink-0 w-10 h-10 bg-green-600 rounded-lg flex items-center justify-center">
                        <i class="fas fa-check-circle text-white"></i>
                    </div>
                    <div>
                        <h3 class="font-semibold text-gray-900 mb-1">Jóváhagyási folyamat</h3>
                        <p class="text-gray-600">Átlátható workflow lépésről lépésre</p>
                    </div>
                </div>

                <!-- Feature 3 -->
                <div class="flex items-start gap-4 p-4 bg-purple-50 rounded-lg">
                    <div class="flex-shrink-0 w-10 h-10 bg-purple-600 rounded-lg flex items-center justify-center">
                        <i class="fas fa-search text-white"></i>
                    </div>
                    <div>
                        <h3 class="font-semibold text-gray-900 mb-1">Gyors keresés</h3>
                        <p class="text-gray-600">Bármilyen dokumentum megtalálása másodpercek alatt</p>
                    </div>
                </div>

                <!-- Feature 4 -->
                <div class="flex items-start gap-4 p-4 bg-orange-50 rounded-lg">
                    <div class="flex-shrink-0 w-10 h-10 bg-orange-600 rounded-lg flex items-center justify-center">
                        <i class="fas fa-comments text-white"></i>
                    </div>
                    <div>
                        <h3 class="font-semibold text-gray-900 mb-1">Megjegyzések</h3>
                        <p class="text-gray-600">Kollégákkal való egyszerű kommunikáció</p>
                    </div>
                </div>

                <!-- Feature 5 -->
                <div class="flex items-start gap-4 p-4 bg-indigo-50 rounded-lg">
                    <div class="flex-shrink-0 w-10 h-10 bg-indigo-600 rounded-lg flex items-center justify-center">
                        <i class="fas fa-chart-bar text-white"></i>
                    </div>
                    <div>
                        <h3 class="font-semibold text-gray-900 mb-1">Áttekintés</h3>
                        <p class="text-gray-600">Minden dokumentum egy helyen, strukturáltan</p>
                    </div>
                </div>
            </div>
        </div>

        <!-- Kezdés -->
        <div class="mb-8">
            <h2 class="text-2xl font-semibold text-gray-900 mb-4">Kezdés</h2>
            
            <ol class="space-y-3">
                <li class="flex items-start gap-3">
                    <span class="flex-shrink-0 w-8 h-8 bg-blue-600 text-white rounded-full flex items-center justify-center font-semibold">1</span>
                    <p class="text-gray-700 pt-1">
                        Töltsd fel az első dokumentumodat a <span class="font-semibold">"+ Új dokumentum"</span> gombbal
                    </p>
                </li>
                <li class="flex items-start gap-3">
                    <span class="flex-shrink-0 w-8 h-8 bg-blue-600 text-white rounded-full flex items-center justify-center font-semibold">2</span>
                    <p class="text-gray-700 pt-1">
                        Nézd meg az <span class="font-semibold">"Aktuális ügyeim"</span> oldalt a rád váró feladatokhoz
                    </p>
                </li>
                <li class="flex items-start gap-3">
                    <span class="flex-shrink-0 w-8 h-8 bg-blue-600 text-white rounded-full flex items-center justify-center font-semibold">3</span>
                    <p class="text-gray-700 pt-1">
                        Ha elakadtál, kérdezz bátran a kollégáidtól! 🚀
                    </p>
                </li>
            </ol>
        </div>

        <!-- CTA Button -->
        <div class="text-center pt-4 border-t border-gray-200">
            <button class="bg-blue-600 hover:bg-blue-700 text-white font-semibold px-8 py-3 rounded-lg transition-colors duration-200 inline-flex items-center gap-2">
                <span>Jó munkát!</span>
                <i class="fas fa-arrow-right"></i>
            </button>
        </div>
		</div>
	</AppLayout>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import AppLayout from '../layout/AppLayout.vue';
import BaseButton from '../base/BaseButton.vue';
import type { Company } from '../../types/user.types';
import { useAuthStore } from '../../stores/authStore';
import { useRouter } from 'vue-router';

const router = useRouter();
const auth = useAuthStore();

const navItems = ref([
	{ label: 'Dashboard', to: '/dashboard', icon: ['fas','gauge'], exact: true },
	{ label: 'Documents', icon: ['fas','file'], children: [
		{ label: 'All Documents', to: '/documents' },
		{ label: 'Create', to: '/documents/new' }
	]},
	{ label: 'Settings', to: '/settings', icon: ['fas','gear'] }
]);

const companies = ref<Company[]>([
	{ Id: 1, Name: 'Acme Inc.', TaxNumber: '12345678', IsActive: true, CreatedAt: new Date().toISOString(), Users: [], Documents: [] },
	{ Id: 2, Name: 'Globex', TaxNumber: '98765432', IsActive: true, CreatedAt: new Date().toISOString(), Users: [], Documents: [] }
]);
const selectedCompanyId = ref<number | null>(1);

function logout() {
	auth.logout();
	router.replace('/login');
}
</script>


